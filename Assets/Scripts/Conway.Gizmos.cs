using UnityEngine;

public partial struct Conway
{
    public void DrawGridBoundries()
    {
        for (int i = 0; i < ArrayElementWidth; i++)
        {
            for (int j = 0; j < ArrayElementHeight; j++)
            {
                var startingX = i * 64;
                var startingY = j;


                Gizmos.DrawWireCube(new Vector3(startingX + 32 - 0.5f, startingY), new Vector3(64, 1.0f));
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

    public void DrawPreviousGrid()
    {
        var previousGrid = PreviousGrid;

        if (previousGrid.IsCreated == false)
            return;

        Gizmos.color = Color.white;

        for (int y = 0; y < ArrayElementHeight; y++)
        {
            for (int x = 0; x < ArrayElementWidth; x++)
            {
                int index = y * ArrayElementWidth + x;

                var baseCells = previousGrid[index];

                for (int i = 0; i < 64; i++)
                {
                    bool isAlive = ((baseCells >> i) & 1) == 1;

                    if (isAlive)
                    {
                        Gizmos.DrawWireCube(new Vector3(x * 64 + i, y, 0), new Vector3(0.75f, 0.75f, 0.75f));
                    }
                }
            }
        }
    }

    public void DrawGridBoundriesSquare()
    {
        for (int i = 0; i < ArrayElementWidth; i++)
        {
            for (int j = 0; j < ArrayElementHeight; j++)
            {
                var startingX = i * 8;
                var startingY = j * 8;


                Gizmos.DrawWireCube(new Vector3(startingX + 4 - 0.5f, startingY + 4 - 0.5f), new Vector3(8, 8));
            }
        }
    }

    public void DrawCurrentGridSquare()
    {
        var currentGrid = CurrentGrid;

        if (currentGrid.IsCreated == false)
            return;

        UnityEngine.Gizmos.color = UnityEngine.Color.red;

        for (int arrayY = 0; arrayY < ArrayElementHeight; arrayY++)
        {
            for (int arrayX = 0; arrayX < ArrayElementWidth; arrayX++)
            {
                int index = arrayY * ArrayElementWidth + arrayX;

                var baseCells = currentGrid[index];

                Vector3 bottomCorner = new Vector3(arrayX * 8, arrayY * 8, 0);

                for (int cellY = 0; cellY < 8; cellY++)
                {
                    for (int cellX = 0; cellX < 8; cellX++)
                    {
                        bool isAlive = ((baseCells >> (cellY * 8 + cellX)) & 1) == 1;

                        if (isAlive)
                        {
                            UnityEngine.Gizmos.DrawWireCube(bottomCorner + new Vector3(cellX, cellY), UnityEngine.Vector3.one);
                        }
                    }
                }
            }
        }
    }

    public void DrawPreviousGridSquare()
    {
        var previousGrid = PreviousGrid;

        if (previousGrid.IsCreated == false)
            return;

        UnityEngine.Gizmos.color = UnityEngine.Color.white;

        for (int arrayY = 0; arrayY < ArrayElementHeight; arrayY++)
        {
            for (int arrayX = 0; arrayX < ArrayElementWidth; arrayX++)
            {
                int index = arrayY * ArrayElementWidth + arrayX;

                var baseCells = previousGrid[index];

                Vector3 bottomCorner = new Vector3(arrayX * 8, arrayY * 8, 0);

                for (int cellY = 0; cellY < 8; cellY++)
                {
                    for (int cellX = 0; cellX < 8; cellX++)
                    {
                        bool isAlive = ((baseCells >> (cellY * 8 + cellX)) & 1) == 1;

                        if (isAlive)
                        {
                            UnityEngine.Gizmos.DrawWireCube(bottomCorner + new Vector3(cellX, cellY), new UnityEngine.Vector3(0.75f, 0.75f, 0.75f));
                        }
                    }
                }
            }
        }
    }
}
