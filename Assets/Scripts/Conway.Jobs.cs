using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public partial struct Conway
{

    [BurstCompile]
    public struct UpdateGridJob : IJobParallelFor
    {
        public int ArrayElementWidth;
        public int ArrayElemetCount;

        const ulong EndingBit = (1UL << 63);
        const ulong BeginningBit = (1UL << 0);

        [ReadOnly] public NativeArray<ulong> BaseGrid;
        [WriteOnly] public NativeArray<ulong> NewGrid;

        public unsafe void Execute(int index)
        {
            var neighborCounts = stackalloc byte[64];

            ulong baseCells = BaseGrid[index];

            var topIndex = index + ArrayElementWidth;
            var bottomIndex = index - ArrayElementWidth;
            var leftIndex = index - 1;
            var rightIndex = index + 1;

            var leftTopIndex = topIndex - 1;
            var rightTopIndex = topIndex + 1;
            var leftBottomIndex = bottomIndex - 1;
            var rightBottomIndex = bottomIndex + 1;

            // If there are cells to the left of this chunk.
            if (leftIndex >= 0)
            {
                ulong left = BaseGrid[leftIndex];
                neighborCounts[0] = (left & BeginningBit) == BeginningBit ? (byte)1 : (byte)0;

                // If there are cells to the bottom left of this chunk
                if (leftBottomIndex >= 0)
                {
                    ulong leftBottom = BaseGrid[leftBottomIndex];
                    neighborCounts[0] += (leftBottom & BeginningBit) == BeginningBit ? (byte)1 : (byte)0;
                }

                // If there are cells to the top left of this chunk
                if (leftTopIndex < ArrayElemetCount)
                {
                    ulong leftTop = BaseGrid[leftTopIndex];
                    neighborCounts[0] += (leftTop & BeginningBit) == BeginningBit ? (byte)1 : (byte)0;
                }
            }

            // If there are cells to the right of this chunk.
            if (rightIndex < ArrayElemetCount)
            {
                ulong right = BaseGrid[rightIndex];
                neighborCounts[63] = (right & EndingBit) == EndingBit ? (byte)1 : (byte)0;

                // If there are cells to the bottom right of this chunk
                if (rightBottomIndex >= 0)
                {
                    ulong rightBottom = BaseGrid[rightBottomIndex];
                    neighborCounts[63] += (rightBottom & EndingBit) == EndingBit ? (byte)1 : (byte)0;
                }

                // If there are cells to the top right of this chunk
                if (rightTopIndex < ArrayElemetCount)
                {
                    ulong rightTop = BaseGrid[rightTopIndex];
                    neighborCounts[63] += (rightTop & EndingBit) == EndingBit ? (byte)1 : (byte)0;
                }
            }

            // Left and right internal.
            {
                for (int i = 1; i < 64 - 1; i += 2)
                {
                    bool isCenterEnabled = ((baseCells >> i) & 1) == 1;

                    neighborCounts[i - 1] += isCenterEnabled ? (byte)1 : (byte)0;
                    neighborCounts[i + 1] += isCenterEnabled ? (byte)1 : (byte)0;

                    bool isLeftEnabled = ((baseCells >> (i - 1)) & 1) == 1;
                    bool isRightEnabled = ((baseCells >> (i + 1)) & 1) == 1;
                    neighborCounts[i] += isLeftEnabled ? (byte)1 : (byte)0;
                    neighborCounts[i] += isRightEnabled ? (byte)1 : (byte)0;
                }
            }

            if (topIndex < ArrayElemetCount)
            {
                ulong top = BaseGrid[topIndex];

                for (int i = 0; i < 64; i++)
                {
                    neighborCounts[i] += ((top >> i) & 1) == 1 ? (byte)1 : (byte)0;
                }

                for (int i = 1; i < 64 - 1; i++)
                {
                    neighborCounts[i] += ((top >> (i - 1)) & 1) == 1 ? (byte)1 : (byte)0;
                    neighborCounts[i] += ((top >> (i + 1)) & 1) == 1 ? (byte)1 : (byte)0;
                }
            }

            if (bottomIndex >= 0)
            {
                ulong bottom = BaseGrid[bottomIndex];

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
                bool isCellAlive = ((baseCells >> i) & 1) == 1;

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
