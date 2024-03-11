using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using System;
using System.Text;

//Standard Rules of Conway's Game of Life:

//Cells are arranged in a square grid
//Cells can be considered live or dead
//Cells live or die each generation based on neighboring cells (up, down, left, right, 4 diagonal corners)
//A generation is a single update of all cells in the simulation
//A live cell with 0 or 1 live neighbors dies next generation due to underpopulation
//A live cell with 2 or 3 live neighbors lives on to the next generation
//A live cell with 4 or more live neighbors dies next generation due to overpopulation
//A dead cell with exactly 3 live neighbors becomes a live cell next generation due to reproduction

// If alive, you live if there is 2 or 3 alive neighbores.
// If dead, you become alive if there is 3 alive neighbors.

[BurstCompile]
public partial struct Conway : IDisposable
{
    public int Resolution;

    public int GridSize;
    public int GridCellCount;

    public int ArrayElementWidth;
    public int ArrayElementHeight;
    public int ArrayElemetCount;

    public int Iteration;

    public NativeArray<ulong> grid0;
    public NativeArray<ulong> grid1;

    public NativeArray<ulong> CurrentGrid => (Iteration % 2 == 0) ? grid0 : grid1;
    public NativeArray<ulong> PreviousGrid => (Iteration % 2 == 0) ? grid1 : grid0;

    public Conway(int resolution, Allocator allocator)
    {
        Resolution = resolution;

        GridSize = (int)math.floor(math.pow(2, resolution) * 64);
        GridCellCount = GridSize * GridSize;

        ArrayElementWidth = GridSize / 64;
        ArrayElementHeight = GridSize;
        ArrayElemetCount = ArrayElementWidth * ArrayElementHeight;

        Iteration = 0;

        grid0 = new NativeArray<ulong>(ArrayElemetCount, allocator);
        grid1 = new NativeArray<ulong>(ArrayElemetCount, allocator);

        RandomGrid();
    }

    public void RandomGrid()
    {
        var createGridJob = new CreateGridJob
        {
            ArrayElementWidth = ArrayElementWidth,
            BaseGrid = grid0,
        };

        createGridJob.Schedule(ArrayElemetCount, 4).Complete();
    }

    public void Update()
    {
        var updateGridJob = new UpdateGridJob
        {
            ArrayElementWidth = ArrayElementWidth, 
            ArrayElementHeight = ArrayElementHeight,
            ArrayElemetCount = ArrayElemetCount,
            BaseGrid = CurrentGrid,
            NewGrid = PreviousGrid,
        };

        Iteration++;

        updateGridJob.Schedule(ArrayElemetCount, 4).Complete();
    }

    public string PrintGrid()
    {
        StringBuilder stringBuilder = new StringBuilder(GridCellCount * 3 + 30);

        var currentGrid = CurrentGrid;

        for (int y = 0; y < ArrayElementHeight; y++)
        {
            for (int x = 0; x < ArrayElementWidth; x++)
            {
                int index = y * ArrayElementWidth + x;

                var baseCells = currentGrid[index];

                for (int i = 0; i < 64; i++)
                {
                    var cellValue = ((baseCells >> i) & 1) == 1 ? 1 : 0;
                    stringBuilder.Append(cellValue);
                    stringBuilder.Append(' ');
                }
            }

            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    public void DrawPreviousGrid()
    {
        var previousGrid = PreviousGrid;

        if (previousGrid.IsCreated == false)
            return;

        UnityEngine.Gizmos.color = UnityEngine.Color.white;

        for (int y = 0; y < ArrayElementHeight; y++)
        {
            for (int x = 0; x < ArrayElementWidth; x++)
            {
                int index = y * ArrayElementWidth + x;

                var baseCells = previousGrid[index];

                for (int i = 0; i < 64; i++)
                {
                    bool isAlive = ((baseCells >> i) & 1) == 1;

                    if(isAlive)
                    {
                        UnityEngine.Gizmos.DrawWireCube(new UnityEngine.Vector3(x * 64 + i, y, 0), new UnityEngine.Vector3(0.75f, 0.75f, 0.75f));
                    }
                }
            }
        }
    }

    public void DrawCurrentGrid()
    {
        var currentGrid = CurrentGrid;

        if (currentGrid.IsCreated == false)
            return;

        UnityEngine.Gizmos.color = UnityEngine.Color.red;

        for (int y = 0; y < ArrayElementHeight; y++)
        {
            for (int x = 0; x < ArrayElementWidth; x++)
            {
                int index = y * ArrayElementWidth + x;

                var baseCells = currentGrid[index];

                for (int i = 0; i < 64; i++)
                {
                    bool isAlive = ((baseCells >> i) & 1) == 1;

                    if (isAlive)
                    {
                        UnityEngine.Gizmos.DrawWireCube(new UnityEngine.Vector3(x * 64 + i, y, 0), UnityEngine.Vector3.one);
                    }
                }
            }
        }
    }

    public int GetNumberOfAliveCells()
    {
        var currentGrid = CurrentGrid;

        if (currentGrid.IsCreated == false)
            return 0;

        int aliveCount = 0;

        for (int y = 0; y < ArrayElementHeight; y++)
        {
            for (int x = 0; x < ArrayElementWidth; x++)
            {
                int index = y * ArrayElementWidth + x;

                var baseCells = currentGrid[index];

                aliveCount += math.countbits(baseCells);
            }
        }

        return aliveCount;
    }

    public void Dispose()
    {
        if (grid0.IsCreated) grid0.Dispose();
        if (grid1.IsCreated) grid1.Dispose();
    }

    [BurstDiscard]
    public override string ToString()
    {
        return $"Conway: Resolution: {Resolution}, GridSize: {GridSize}, GridCellCount: {GridCellCount}, ArrayElementWidth: {ArrayElementWidth}, ArrayElementHeight: {ArrayElementHeight}, ArrayElemetCount: {ArrayElemetCount}, Iteration: {Iteration}";
    }
}
