namespace SudokuSolver.Models;

internal sealed partial class CellList : IReadOnlyList<Cell>
{
    private const int cLength = 81;

    private readonly Cell[] cells = new Cell[cLength];

    public bool Rotated { get; set; } = false;


    public CellList()
    {
        for (int index = 0; index < cLength; index++)
        {
            cells[index] = new Cell(index);
        }
    }

    public CellList(CellList source)
    {
        for (int index = 0; index < cLength; index++)
        {
            cells[index] = new Cell(source.cells[index]);
        }
    }


    public Cell this[int index]
    {
        get
        {
            Debug.Assert(!Rotated);
            return cells[index];
        }
    }


    private Cell this[int x, int y]
    {
        get
        {
            if (Rotated)
            {
                return cells[Convert(y, x)];
            }

            return cells[Convert(x, y)];
        }
    }


    private static int Convert(int x, int y) => x + (y * 9);


    public void CopyFrom(CellList other)
    {
        for (int index = 0; index < cLength; index++)
        {
            cells[index].CopyFrom(other.cells[index]);
        }
    }


    public IEnumerable<Cell> Row(int rowIndex)
    {
        for (int x = 0; x < 9; x++)
        {
            yield return this[x, rowIndex];
        }
    }


    public IEnumerable<Cell> Column(int columnIndex)
    {
        for (int y = 0; y < 9; y++)
        {
            yield return this[columnIndex, y];
        }
    }



    public IEnumerable<Cell> RowMinus(int cubeX, int rowIndex)
    {
        // the row apart from cells in the cube

        if (cubeX == 0) // cube is at the start of the row
        {
            for (int x = 3; x < 9; x++)
            {
                yield return this[x, rowIndex];
            }
        }
        else if (cubeX == 2) // cube is at the end of the row
        {
            for (int x = 0; x < 6; x++)
            {
                yield return this[x, rowIndex];
            }
        }
        else
        {
            for (int x = 0; x < 3; x++)
            {
                yield return this[x, rowIndex];
            }

            for (int x = 6; x < 9; x++)
            {
                yield return this[x, rowIndex];
            }
        }
    }



    public IEnumerable<Cell> ColumnMinus(int cubeY, int columnIndex)
    {
        // the column apart from cells in the cube

        if (cubeY == 0) // cube is at top of the column
        {
            for (int y = 3; y < 9; y++)
            {
                yield return this[columnIndex, y];
            }
        }
        else if (cubeY == 2) // cube is at bottom of the column
        {
            for (int y = 0; y < 6; y++)
            {
                yield return this[columnIndex, y];
            }
        }
        else
        {
            for (int y = 0; y < 3; y++)
            {
                yield return this[columnIndex, y];
            }

            for (int y = 6; y < 9; y++)
            {
                yield return this[columnIndex, y];
            }
        }
    }



    public IEnumerable<Cell> CubeColumn(int cubeX, int cubeY, int columnIndex)
    {
        int x = (cubeX * 3) + columnIndex;
        int startY = cubeY * 3;

        for (int y = 0; y < 3; y++)
        {
            yield return this[x, startY + y];
        }
    }


    public IEnumerable<Cell> CubeRow(int cubeX, int cubeY, int rowIndex)
    {
        int y = (cubeY * 3) + rowIndex;
        int startX = cubeX * 3;

        for (int x = 0; x < 3; x++)
        {
            yield return this[startX + x, y];
        }
    }


    public IEnumerable<Cell> Cube(int cubeX, int cubeY)
    {
        int startX = cubeX * 3;
        int startY = cubeY * 3;

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                yield return this[startX + x, startY + y];
            }
        }
    }

    // a cube has 9 values minus one - the source cell 
    // a row has a further 6 to the left or right of the cube
    // a column has 6 to the top or bottom of the cube
    // a total of 20 cells per enumeration
    public IEnumerable<Cell> CubeRowColumnMinus(int sourceCellIndex)
    {
        int row = sourceCellIndex / 9;
        int column = sourceCellIndex % 9;
        int cubeY = row / 3;
        int cubeX = column / 3;

        // the cube minus the source cell
        foreach (Cell cell in Cube(cubeX, cubeY))
        {
            if (cell.Index != sourceCellIndex)
            {
                yield return cell;
            }
        }

        // the row apart from cells already in the cube
        foreach (Cell cell in RowMinus(cubeX, row))
        {
            yield return cell;
        }

        // the column apart from cells already in the cube
        foreach (Cell cell in ColumnMinus(cubeY, column))
        {
            yield return cell;
        }
    }


    public IEnumerator<Cell> GetEnumerator()
    {
        for (int index = 0; index < cLength; index++)
        {
            yield return cells[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => cLength;
}
