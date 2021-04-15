using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

namespace Sudoku.Models
{

    internal sealed class CellList : IReadOnlyList<Cell>, IEquatable<CellList>
    {
        private const int cLength = 81;

        private readonly Cell[] cells = new Cell[cLength];

        public bool Rotated { get; set; } = false;


        public CellList()
        {
            for (int index = 0; index < cLength; index++)
                cells[index] = new Cell(index);
        }

        public CellList(CellList source)
        {
            for (int index = 0; index < cLength; index++)
                cells[index] = new Cell(source.cells[index]);
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
                    return cells[Convert(y, x)];

                return cells[Convert(x, y)];
            }
        }


        private static int Convert(int x, int y) => x + (y * 9);


        public void CopyFrom(CellList other)
        {
            for (int index = 0; index < cLength; index++)
                cells[index].CopyFrom(other.cells[index], index);
        }


        public IEnumerable<Cell> Row(int rowIndex)
        {
            for (int x = 0; x < 9; x++)
                yield return this[x, rowIndex];
        }


        public IEnumerable<Cell> Column(int columnIndex)
        {
            for (int y = 0; y < 9; y++)
                yield return this[columnIndex, y];
        }



        public IEnumerable<Cell> RowMinus(int cubex, int rowIndex)
        {
            // the row apart from cells in the cube

            if (cubex == 0) // cube is at the start of the row
            {
                for (int x = 3; x < 9; x++)
                    yield return this[x, rowIndex];
            }
            else if (cubex == 2) // cube is at the end of the row
            {
                for (int x = 0; x < 6; x++)
                    yield return this[x, rowIndex];
            }
            else
            {
                for (int x = 0; x < 3; x++)
                    yield return this[x, rowIndex];

                for (int x = 6; x < 9; x++)
                    yield return this[x, rowIndex];
            }
        }



        public IEnumerable<Cell> ColumnMinus(int cubey, int columnIndex)
        {
            // the column apart from cells in the cube

            if (cubey == 0) // cube is at top of the column
            {
                for (int y = 3; y < 9; y++)
                    yield return this[columnIndex, y];
            }
            else if (cubey == 2) // cube is at bottom of the column
            {
                for (int y = 0; y < 6; y++)
                    yield return this[columnIndex, y];
            }
            else
            {
                for (int y = 0; y < 3; y++)
                    yield return this[columnIndex, y];

                for (int y = 6; y < 9; y++)
                    yield return this[columnIndex, y];
            }
        }



        public IEnumerable<Cell> CubeColumn(int cubex, int cubey, int columnIndex)
        {
            int x = (cubex * 3) + columnIndex;
            int startY = cubey * 3;

            for (int y = 0; y < 3; y++)
                yield return this[x, startY + y];
        }


        public IEnumerable<Cell> CubeRow(int cubex, int cubey, int rowIndex)
        {
            int y = (cubey * 3) + rowIndex;
            int startX = cubex * 3;

            for (int x = 0; x < 3; x++)
                yield return this[startX + x, y];
        }



        // a cube has 9 values minus one - the source cell 
        // a row has a further 6 to the left or right of the cube
        // a column has 6 to the top or bottom of the cube
        // a total of 20 cells per enumeration
        public IEnumerable<Cell> CubeRowColumnMinus(int sourceCellIndex)
        {
            int row = sourceCellIndex / 9;
            int column = sourceCellIndex % 9;
            int cubey = row / 3;
            int cubex = column / 3;
            int startX = cubex * 3;
            int startY = cubey * 3;

            // the cube minus the source cell
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Cell cell = this[startX + x, startY + y];

                    if (cell.Index != sourceCellIndex)
                        yield return cell;
                }
            }

            // the row apart from cells already in the cube
            foreach (Cell cell in RowMinus(cubex, row))
                yield return cell;

            // the column apart from cells already in the cube
            foreach (Cell cell in ColumnMinus(cubey, column))
                yield return cell;
        }


        public IEnumerator<Cell> GetEnumerator()
        {
            for (int index = 0; index < cLength; index++)
                yield return cells[index];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => cLength;

        public bool Equals(CellList? other)
        {
            if (other is null)
                return false;

            return cells.AsSpan().SequenceEqual(other.cells);
        }

        public override bool Equals(object? obj) => Equals(obj as CellList);

        public override int GetHashCode() => throw new NotImplementedException();
    }
}
