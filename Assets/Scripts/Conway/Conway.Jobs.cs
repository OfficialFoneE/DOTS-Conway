using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

public partial struct Conway
{
    // The bits in a ulong represent an 8x8 grid of cells.
    // 0 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0 0

    // 56 57 58 59 60 61 62 63
    // 48 49 50 51 52 53 54 55
    // 40 41 42 43 44 45 46 47
    // 32 33 34 35 36 37 38 39
    // 24 25 26 27 28 29 30 31
    // 16 17 18 19 20 21 22 23
    //  8  9 10 11 12 13 14 15
    //  0  1  2  3  4  5  6  7

    [BurstCompile]
    public struct UpdateGridJobBatchSquare : IJobParallelForBatch
    {
        public int ArrayElementWidth;
        public int ArrayElementHeight;
        public int ArrayElemetCount;

        [ReadOnly] public NativeArray<ulong> BaseGrid;
        [WriteOnly] public NativeArray<ulong> NewGrid;

        private static int GetCount(ulong value, ulong mask)
        {
            return math.countbits(value & mask);
        }
        private static bool CalculateNewCellValue(ulong value, int index, ulong mask)
        {
            var count = GetCount(value, mask);

            bool isCellAlive = ((value >> index) & 1) == 1;
            bool isNewCellAlive = isCellAlive ? (count == 2 | count == 3) : count == 3;

            return isNewCellAlive;
        }

        public unsafe void Execute(int startIndex, int count)
        {
            var neighborCounts = stackalloc byte[64];

            int startX = startIndex % ArrayElementWidth;
            int startY = startIndex / ArrayElementWidth;

            int x = startX;
            int y = startY;

            for (int index = 0; index < count; index++)
            {
                int currentIndex = startIndex + index;

                ulong baseCells = BaseGrid[currentIndex];

                for (int i = 0; i < 64; i++)
                {
                    neighborCounts[i] = (byte)math.countbits(baseCells & Tables.NeighborLookups[i]);
                }

                bool isLeft = x - 1 >= 0;
                bool isRight = x + 1 < ArrayElementWidth;
                bool isTop = y + 1 < ArrayElementHeight;
                bool isBottom = y - 1 >= 0;

                var topIndex = currentIndex + ArrayElementWidth;
                var bottomIndex = currentIndex - ArrayElementWidth;
                var leftIndex = currentIndex - 1;
                var rightIndex = currentIndex + 1;

                var leftTopIndex = topIndex - 1;
                var rightTopIndex = topIndex + 1;
                var leftBottomIndex = bottomIndex - 1;
                var rightBottomIndex = bottomIndex + 1;

                // If there are cells to the left of this chunk.
                if (Hint.Likely(isLeft))
                {
                    var leftMask = BaseGrid[leftIndex];

                    neighborCounts[0] += GetBitValue(leftMask, 7);
                    neighborCounts[0] += GetBitValue(leftMask, 15);

                    neighborCounts[56] += GetBitValue(leftMask, 63);
                    neighborCounts[56] += GetBitValue(leftMask, 55);

                    for (int i = 1; i < 7; i++)
                    {
                        neighborCounts[i * 8] += GetBitValue(leftMask, (i - 1) * 8 + 7);
                        neighborCounts[i * 8] += GetBitValue(leftMask, i * 8 + 7);
                        neighborCounts[i * 8] += GetBitValue(leftMask, (i + 1) * 8 + 7);
                    }

                    // If there are cells to the bottom left of this chunk
                    if (Hint.Likely(isBottom))
                    {
                        neighborCounts[0] += GetBitValue(BaseGrid[leftBottomIndex], 63);
                    }

                    // If there are cells to the top left of this chunk
                    if (Hint.Likely(isTop))
                    {
                        neighborCounts[56] += GetBitValue(BaseGrid[leftTopIndex], 7);
                    }
                }

                // If there are cells to the right of this chunk.
                if (Hint.Likely(isRight))
                {
                    var rightMask = BaseGrid[rightIndex];

                    neighborCounts[7] += GetBitValue(rightMask, 0);
                    neighborCounts[7] += GetBitValue(rightMask, 8);

                    neighborCounts[63] += GetBitValue(rightMask, 56);
                    neighborCounts[63] += GetBitValue(rightMask, 48);

                    for (int i = 1; i < 7; i++)
                    {
                        neighborCounts[i * 8 + 7] += GetBitValue(rightMask, (i - 1) * 8);
                        neighborCounts[i * 8 + 7] += GetBitValue(rightMask, i * 8);
                        neighborCounts[i * 8 + 7] += GetBitValue(rightMask, (i + 1) * 8);
                    }

                    // If there are cells to the bottom right of this chunk
                    if (Hint.Likely(isBottom))
                    {
                        neighborCounts[7] += GetBitValue(BaseGrid[rightBottomIndex], 56);
                    }

                    // If there are cells to the top right of this chunk
                    if (Hint.Likely(isTop))
                    {
                        neighborCounts[63] += GetBitValue(BaseGrid[rightTopIndex], 0);
                    }
                }

                if (Hint.Likely(isTop))
                {
                    ulong top = BaseGrid[topIndex];

                    neighborCounts[56] += GetBitValue(top, 0);
                    neighborCounts[56] += GetBitValue(top, 1);

                    neighborCounts[63] += GetBitValue(top, 6);
                    neighborCounts[63] += GetBitValue(top, 7);

                    for (int i = 1; i < 7; i++)
                    {
                        neighborCounts[i + 56] += GetBitValue(top, i - 1);
                        neighborCounts[i + 56] += GetBitValue(top, i);
                        neighborCounts[i + 56] += GetBitValue(top, i + 1);
                    }
                }

                if (Hint.Likely(isBottom))
                {
                    ulong bottom = BaseGrid[bottomIndex];

                    neighborCounts[0] += GetBitValue(bottom, 56);
                    neighborCounts[0] += GetBitValue(bottom, 57);

                    neighborCounts[7] += GetBitValue(bottom, 62);
                    neighborCounts[7] += GetBitValue(bottom, 63);

                    for (int i = 1; i < 7; i++)
                    {
                        neighborCounts[i] += GetBitValue(bottom, (i - 1) + 56);
                        neighborCounts[i] += GetBitValue(bottom, i + 56);
                        neighborCounts[i] += GetBitValue(bottom, (i + 1) + 56);
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

                NewGrid[currentIndex] = results;

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
    public struct UpdateGridJobBatch : IJobParallelForBatch
    {
        public int ArrayElementWidth;
        public int ArrayElementHeight;
        public int ArrayElemetCount;

        [ReadOnly] public NativeArray<ulong> BaseGrid;
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
                int currentIndex = startIndex + index;

                ulong baseCells = BaseGrid[currentIndex];

                bool isLeft = x - 1 >= 0;
                bool isRight = x + 1 < ArrayElementWidth;
                bool isTop = y + 1 < ArrayElementHeight;
                bool isBottom = y - 1 >= 0;

                var topIndex = currentIndex + ArrayElementWidth;
                var bottomIndex = currentIndex - ArrayElementWidth;
                var leftIndex = currentIndex - 1;
                var rightIndex = currentIndex + 1;

                var leftTopIndex = topIndex - 1;
                var rightTopIndex = topIndex + 1;
                var leftBottomIndex = bottomIndex - 1;
                var rightBottomIndex = bottomIndex + 1;

                neighborCounts[0] += GetBitValue(baseCells, 1);

                // If there are cells to the left of this chunk.
                if (Hint.Likely(isLeft))
                {
                    neighborCounts[0] += GetBitValue(BaseGrid[leftIndex], 63);

                    // If there are cells to the bottom left of this chunk
                    if (Hint.Likely(isBottom))
                    {
                        neighborCounts[0] += GetBitValue(BaseGrid[leftBottomIndex], 63);
                    }

                    // If there are cells to the top left of this chunk
                    if (Hint.Likely(isTop))
                    {
                        neighborCounts[0] += GetBitValue(BaseGrid[leftTopIndex], 63);
                    }
                }

                neighborCounts[63] += GetBitValue(baseCells, 62);

                // If there are cells to the right of this chunk.
                if (Hint.Likely(isRight))
                {
                    neighborCounts[63] += GetBitValue(BaseGrid[rightIndex], 0);

                    // If there are cells to the bottom right of this chunk
                    if (Hint.Likely(isBottom))
                    {
                        neighborCounts[63] += GetBitValue(BaseGrid[rightBottomIndex], 0);
                    }

                    // If there are cells to the top right of this chunk
                    if (Hint.Likely(isTop))
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

                if (Hint.Likely(isTop))
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

                if (Hint.Likely(isBottom))
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

                NewGrid[currentIndex] = results;

                x++;
                if (x >= ArrayElementWidth)
                {
                    y++;
                    x = 0;
                }

                UnsafeUtility.MemClear(neighborCounts, UnsafeUtility.SizeOf<byte>() * 64);
            }
        }

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
                bool isAlive = noise.snoise(new float2(x * 64 + i, y)) > 0.4f;

                results |= (isAlive ? 1UL : 0UL) << i;
            }

            BaseGrid[index] = results;
        }
    }
}
