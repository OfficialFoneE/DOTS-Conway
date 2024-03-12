using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

public partial struct Conway
{
    [BurstCompile]
    public struct UpdateGridJobBatch : IJobParallelForBatch
    {
        public int ArrayElementWidth;
        public int ArrayElementHeight;
        public int ArrayElemetCount;

        [ReadOnly] public NativeArray<ulong> BaseGrid;
        [NativeDisableParallelForRestriction]
        [NoAlias]
        [WriteOnly] public NativeArray<ulong> NewGrid;

        public unsafe void Execute(int startIndex, int count)
        {
            var neighborCounts = stackalloc byte[64];

            int startX = startIndex % ArrayElementWidth;
            int startY = startIndex / ArrayElementWidth;

            int x = startX;
            int y = startY;

            for (int index = 0; index < count; index++)
            {
                int arrayIndex = startIndex + index;


                int mortonIndex = GetMortonIndex(x, y);

                ulong baseCells = BaseGrid[mortonIndex];

                bool isLeft = x - 1 >= 0;
                bool isRight = x + 1 < ArrayElementWidth;
                bool isTop = y + 1 < ArrayElementHeight;
                bool isBottom = y - 1 >= 0;

                //var topIndex = currentIndex + ArrayElementWidth;
                //var bottomIndex = currentIndex - ArrayElementWidth;
                //var leftIndex = currentIndex - 1;
                //var rightIndex = currentIndex + 1;

                //var leftTopIndex = topIndex - 1;
                //var rightTopIndex = topIndex + 1;
                //var leftBottomIndex = bottomIndex - 1;
                //var rightBottomIndex = bottomIndex + 1;

                neighborCounts[0] += GetBitValue(baseCells, 1);

                // If there are cells to the left of this chunk.

                if (isLeft)
                {
                    var leftIndex = GetMortonLeft(mortonIndex);

                    neighborCounts[0] += GetBitValue(BaseGrid[leftIndex], 63);

                    // If there are cells to the bottom left of this chunk
                    if (isBottom)
                    {
                        var leftBottomIndex = GetMortonBottom(leftIndex);

                        neighborCounts[0] += GetBitValue(BaseGrid[leftBottomIndex], 63);
                    }

                    // If there are cells to the top left of this chunk
                    if (isTop)
                    {
                        var leftTopIndex = GetMortonTop(leftIndex);

                        neighborCounts[0] += GetBitValue(BaseGrid[leftTopIndex], 63);
                    }
                }

                neighborCounts[63] += GetBitValue(baseCells, 62);

                // If there are cells to the right of this chunk.
                if (isRight)
                {
                    var rightIndex = GetMortonRight(mortonIndex);

                    neighborCounts[63] += GetBitValue(BaseGrid[rightIndex], 0);

                    // If there are cells to the bottom right of this chunk
                    if (isBottom)
                    {
                        var rightBottomIndex = GetMortonBottom(rightIndex);

                        neighborCounts[63] += GetBitValue(BaseGrid[rightBottomIndex], 0);
                    }

                    // If there are cells to the top right of this chunk
                    if (isTop)
                    {
                        var rightTopIndex = GetMortonTop(rightIndex);

                        neighborCounts[63] += GetBitValue(BaseGrid[rightTopIndex], 0);
                    }
                }

                // Left and right internal.
                {
                    for (int i = 1; i < 64 - 1; i += 2)
                    {
                        bool isCenterEnabled = IsBitEnabled(baseCells, i);
                        bool isLeftEnabled = IsBitEnabled(baseCells, i - 1);
                        bool isRightEnabled = IsBitEnabled(baseCells, i + 1);

                        neighborCounts[i - 1] += isCenterEnabled ? (byte)1 : (byte)0;
                        neighborCounts[i + 1] += isCenterEnabled ? (byte)1 : (byte)0;
                        neighborCounts[i] += isLeftEnabled ? (byte)1 : (byte)0;
                        neighborCounts[i] += isRightEnabled ? (byte)1 : (byte)0;
                    }
                }

                if (isTop)
                {
                    var topIndex = GetMortonTop(mortonIndex);

                    ulong top = BaseGrid[topIndex];

                    neighborCounts[0] += GetBitValue(top, 1);
                    neighborCounts[63] += GetBitValue(top, 62);

                    // The direct top.
                    for (int i = 0; i < 64; i++)
                    {
                        neighborCounts[i] += ((top >> i) & 1) == 1 ? (byte)1 : (byte)0;
                    }

                    // The diagonals.
                    for (int i = 1; i < 64 - 1; i++)
                    {
                        neighborCounts[i] += ((top >> (i - 1)) & 1) == 1 ? (byte)1 : (byte)0;
                        neighborCounts[i] += ((top >> (i + 1)) & 1) == 1 ? (byte)1 : (byte)0;
                    }
                }

                if (isBottom)
                {
                    var bottomIndex = GetMortonBottom(mortonIndex);

                    ulong bottom = BaseGrid[bottomIndex];

                    neighborCounts[0] += GetBitValue(bottom, 1);
                    neighborCounts[63] += GetBitValue(bottom, 62);

                    for (int i = 0; i < 64; i++)
                    {
                        neighborCounts[i] += ((bottom >> i) & 1) == 1 ? (byte)1 : (byte)0;
                    }

                    for (int i = 1; i < 64 - 1; i++)
                    {
                        neighborCounts[i] += ((bottom >> (i - 1)) & 1) == 1 ? (byte)1 : (byte)0;
                        neighborCounts[i] += ((bottom >> (i + 1)) & 1) == 1 ? (byte)1 : (byte)0;
                    }
                }

                ulong results = 0;

                for (int i = 0; i < 64; i++)
                {
                    bool isCellAlive = IsBitEnabled(baseCells, i);

                    if (isCellAlive)
                    {
                        bool isAlive = neighborCounts[i] == 2 | neighborCounts[i] == 3;

                        results |= (isAlive ? 1UL : 0UL) << i;
                    }
                    else
                    {
                        bool isAlive = neighborCounts[i] == 3;

                        results |= (isAlive ? 1UL : 0UL) << i;
                    }
                }

                NewGrid[mortonIndex] = results;

                x++;
                if (x >= ArrayElementWidth)
                {
                    y++;
                    x = 0;
                }

                UnsafeUtility.MemClear(neighborCounts, UnsafeUtility.SizeOf<byte>() * 64);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMortonTop(int index)
        {
            return (((index & 0b10101010) - 1) &0b10101010) | (index & 0b01010101);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMortonBottom(int index)
        {
            return (((index | 0b01010101) + 1) & 0b10101010) | (index & 0b01010101);

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMortonLeft(int index)
        {
            return (((index & 0b01010101) - 1) &0b01010101) | (index & 0b10101010);

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMortonRight(int index)
        {
            return (((index | 0b10101010) + 1) & 0b01010101) | (index & 0b10101010);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMortonIndex(int x, int y)
        {
            return SwizzleBitsFast(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SwizzleBitsFast(int x, int y)
        {
            int xx = x;
            int yy = y;

            xx = (xx | (xx << 8)) & 0x00FF00FF;
            xx = (xx | (xx << 4)) & 0x0F0F0F0F;
            xx = (xx | (xx << 2)) & 0x33333333;
            xx = (xx | (xx << 1)) & 0x55555555;

            yy = (yy | (yy << 8)) & 0x00FF00FF;
            yy = (yy | (yy << 4)) & 0x0F0F0F0F;
            yy = (yy | (yy << 2)) & 0x33333333;
            yy = (yy | (yy << 1)) & 0x55555555;

            return xx | (yy << 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBitEnabled(ulong value, int index)
        {
            return ((value >> index) & 1) == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetBitValue(ulong value, int index)
        {
            return IsBitEnabled(value, index) ? (byte)1 : (byte)0;
        }
    }

    [BurstCompile]
    public struct UpdateGridJob : IJobParallelFor
    {
        public int ArrayElementWidth;
        public int ArrayElementHeight;
        public int ArrayElemetCount;

        const ulong EndingBit = (1UL << 63);
        const ulong BeginningBit = (1UL << 0);

        [ReadOnly] public NativeArray<ulong> BaseGrid;
        [WriteOnly] public NativeArray<ulong> NewGrid;

        public unsafe void Execute(int index)
        {
            var neighborCounts = stackalloc byte[64];

            ulong baseCells = BaseGrid[index];

            int x = index % ArrayElementWidth;
            int y = index / ArrayElementWidth;

            bool isLeft = x - 1 >= 0;
            bool isRight = x + 1 < ArrayElementWidth;
            bool isTop = y + 1 < ArrayElementHeight;
            bool isBottom = y - 1 >= 0;

            var topIndex = index + ArrayElementWidth;
            var bottomIndex = index - ArrayElementWidth;
            var leftIndex = index - 1;
            var rightIndex = index + 1;

            var leftTopIndex = topIndex - 1;
            var rightTopIndex = topIndex + 1;
            var leftBottomIndex = bottomIndex - 1;
            var rightBottomIndex = bottomIndex + 1;

            neighborCounts[0] += GetBitValue(baseCells, 1);

            // If there are cells to the left of this chunk.
            if (isLeft)
            {
                neighborCounts[0] += GetBitValue(BaseGrid[leftIndex], 63);

                // If there are cells to the bottom left of this chunk
                if (isBottom)
                {
                    neighborCounts[0] += GetBitValue(BaseGrid[leftBottomIndex], 63);
                }

                // If there are cells to the top left of this chunk
                if (isTop)
                {
                    neighborCounts[0] += GetBitValue(BaseGrid[leftTopIndex], 63);
                }
            }

            neighborCounts[63] += GetBitValue(baseCells, 62);

            // If there are cells to the right of this chunk.
            if (isRight)
            {
                neighborCounts[63] += GetBitValue(BaseGrid[rightIndex], 0);

                // If there are cells to the bottom right of this chunk
                if (isBottom)
                {
                    neighborCounts[63] += GetBitValue(BaseGrid[rightBottomIndex], 0);
                }

                // If there are cells to the top right of this chunk
                if (isTop)
                {
                    neighborCounts[63] += GetBitValue(BaseGrid[rightTopIndex], 0);
                }
            }

            // Left and right internal.
            {
                for (int i = 1; i < 64 - 1; i += 2)
                {
                    bool isCenterEnabled = IsBitEnabled(baseCells, i);
                    bool isLeftEnabled = IsBitEnabled(baseCells, i - 1);
                    bool isRightEnabled = IsBitEnabled(baseCells, i + 1);

                    neighborCounts[i - 1] += isCenterEnabled ? (byte)1 : (byte)0;
                    neighborCounts[i + 1] += isCenterEnabled ? (byte)1 : (byte)0;
                    neighborCounts[i] += isLeftEnabled ? (byte)1 : (byte)0;
                    neighborCounts[i] += isRightEnabled ? (byte)1 : (byte)0;
                }
            }

            if (isTop)
            {
                ulong top = BaseGrid[topIndex];

                neighborCounts[0] += GetBitValue(top, 1);
                neighborCounts[63] += GetBitValue(top, 62);

                // The direct top.
                for (int i = 0; i < 64; i++)
                {
                    neighborCounts[i] += ((top >> i) & 1) == 1 ? (byte)1 : (byte)0;
                }

                // The diagonals.
                for (int i = 1; i < 64 - 1; i++)
                {
                    neighborCounts[i] += ((top >> (i - 1)) & 1) == 1 ? (byte)1 : (byte)0;
                    neighborCounts[i] += ((top >> (i + 1)) & 1) == 1 ? (byte)1 : (byte)0;
                }
            }

            if (isBottom)
            {
                ulong bottom = BaseGrid[bottomIndex];

                neighborCounts[0] += GetBitValue(bottom, 1);
                neighborCounts[63] += GetBitValue(bottom, 62);

                for (int i = 0; i < 64; i++)
                {
                    neighborCounts[i] += ((bottom >> i) & 1) == 1 ? (byte)1 : (byte)0;
                }

                for (int i = 1; i < 64 - 1; i++)
                {
                    neighborCounts[i] += ((bottom >> (i - 1)) & 1) == 1 ? (byte)1 : (byte)0;
                    neighborCounts[i] += ((bottom >> (i + 1)) & 1) == 1 ? (byte)1 : (byte)0;
                }
            }

            ulong results = 0;

            for (int i = 0; i < 64; i++)
            {
                bool isCellAlive = IsBitEnabled(baseCells, i);

                if (isCellAlive)
                {
                    bool isAlive = neighborCounts[i] == 2 | neighborCounts[i] == 3;

                    results |= (isAlive ? 1UL : 0UL) << i;
                }
                else
                {
                    bool isAlive = neighborCounts[i] == 3;

                    results |= (isAlive ? 1UL : 0UL) << i;
                }
            }

            NewGrid[index] = results;

            //v128 top = new v128();
            //v128 bottom = new v128();
            //v128 center = new v128();
            //v128 left = new v128();
            //v128 right = new v128();

            //tmp = (input ^ (input >> 8)) & 0x0000ff00;
            //input ^= (tmp ^ (tmp << 8));
            //tmp = (input ^ (input >> 4)) & 0x00f000f0;
            //input ^= (tmp ^ (tmp << 4));
            //tmp = (input ^ (input >> 2)) & 0x0c0c0c0c;
            //input ^= (tmp ^ (tmp << 2));
            //tmp = (input ^ (input >> 1)) & 0x22222222;
            //input ^= (tmp ^ (tmp << 1));

            // Maybe we do 64 x 64 chunks.
            // Than calculate the alive for the bounding chunks?
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBitEnabled(ulong value, int index)
        {
            return ((value >> index) & 1) == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetBitValue(ulong value, int index)
        {
            return IsBitEnabled(value, index) ? (byte)1 : (byte)0;
        }
    }

    [BurstCompile]
    public struct CreateGridJob : IJobParallelFor
    {
        public int ArrayElementWidth;

        [WriteOnly] public NativeArray<ulong> BaseGrid;

        public unsafe void Execute(int index)
        {
            ulong results = 0;

            int x = index % ArrayElementWidth;
            int y = index / ArrayElementWidth;

            for (int i = 0; i < 64; i++)
            {
                bool isAlive = noise.snoise(new float2(x * 64 + i, y)) > 0.2f;

                results |= (isAlive ? 1UL : 0UL) << i;
            }

            BaseGrid[index] = results;
        }
    }
}
