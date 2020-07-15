using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

using Sudoku.Common;


namespace Sudoku.Models
{
    internal sealed class PuzzleModel
    {
        private const string xmlSudokuElementName = "Sudoku";
        private const string xmlCellElementName = "Cell";
        private const string xmlXAttrbuteName = "x";
        private const string xmlYAttrbuteName = "y";

        private enum Process { Rows, Columns }


        public CellList Cells { get; }


        public PuzzleModel()
        {
            Cells = new CellList();
        }






        public void Save(Stream stream)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                OmitXmlDeclaration = true
            };

            using XmlWriter writer = XmlWriter.Create(stream, settings);

            writer.WriteStartElement(xmlSudokuElementName);

            foreach (Cell cell in Cells)
            {
                if (cell.HasValue)
                {
                    writer.WriteStartElement(xmlCellElementName);
                    writer.WriteAttributeString(xmlXAttrbuteName, (cell.Index % 9).ToString());
                    writer.WriteAttributeString(xmlYAttrbuteName, (cell.Index / 9).ToString());
                    writer.WriteValue(cell.Value);
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }


        public void Clear()
        {
            foreach (Cell cell in Cells)
            {
                cell.Value = 0;
                cell.Possibles.Reset(true);
                cell.HorizontalDirections.Reset(false);
                cell.VerticalDirections.Reset(false);
            }
        }


        public void Open(Stream stream)
        {
            Clear();

            using XmlReader reader = XmlReader.Create(stream);

            try
            {
                reader.ReadStartElement(xmlSudokuElementName);

                while (reader.Read())
                {
                    if (reader.Name == xmlCellElementName)
                    {
                        if (int.TryParse(reader.GetAttribute(xmlXAttrbuteName), out int x) && (x >= 0) && (x < 9))
                        {
                            if (int.TryParse(reader.GetAttribute(xmlYAttrbuteName), out int y) && (y >= 0) && (y < 9))
                            {
                                int value = reader.ReadElementContentAsInt();
                           
                                if ((value > 0) && (value < 10))
                                {
                                    int index = x + (y * 9);

                                    if (!Cells[index].HasValue && ValidateCellValue(index, value))
                                        SetCellValue(index, value);
                                }
                            }
                        }
                    }
                }
            }
            catch (XmlException e)
            {
                Debug.Fail(e.Message);
            }
        }


        public bool ValidateCellValue(int index, int newValue)
        {
            Cell cell = Cells[index];

            if (cell.HasValue && (newValue > 0))
            {
                Debug.Fail("replacing a cell value directly isn't supported");
                return false;
            }

            if (newValue == 0) // deleting is always valid
                return true;

            return cell.Possibles[newValue];   // so far at least...
        }



        public void SetCellValue(int index, int newValue)
        {
            Cells[index].Value = newValue;
            CalculatePossibleValues();
        }



        private void SimpleEliminationForCell(Cell updatedCell, Stack<Cell> cellsToUpdate)
        {
            int newValue;

            Debug.Assert(updatedCell.HasValue || (updatedCell.Possibles.Count == 1));

            if (updatedCell.HasValue)
                newValue = updatedCell.Value;
            else
                newValue = updatedCell.Possibles.First;

            foreach (Cell cell in Cells.CubeRowColumnMinus(updatedCell.Index))
            {
                if (!cell.HasValue && cell.Possibles[newValue] && (cell.Possibles.Count > 1))
                {
                    cell.Possibles[newValue] = false;

                    if (cell.Possibles.Count == 1)
                        cellsToUpdate.Push(cell);
                }
            }
        }



        private void UpdateCubesDirections()
        {
            // for each cube...
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    BitField a = AggregateCubeColumnPossibles(x, y, 0);
                    BitField b = AggregateCubeColumnPossibles(x, y, 1);
                    BitField c = AggregateCubeColumnPossibles(x, y, 2);

                    UpdateCubeColumnDirections(x, y, 0, b, c);
                    UpdateCubeColumnDirections(x, y, 1, a, c);
                    UpdateCubeColumnDirections(x, y, 2, a, b);

                    a = AggregateCubeRowPossibles(x, y, 0);
                    b = AggregateCubeRowPossibles(x, y, 1);
                    c = AggregateCubeRowPossibles(x, y, 2);

                    UpdateCubeRowDirections(x, y, 0, b, c);
                    UpdateCubeRowDirections(x, y, 1, a, c);
                    UpdateCubeRowDirections(x, y, 2, a, b);
                }
            }
        }



        private BitField AggregateCubeColumnPossibles(int cubex, int cubey, int column)
        {
            BitField aggregate = new BitField();

            foreach (Cell cell in Cells.CubeColumn(cubex, cubey, column))
            {
                if (!cell.HasValue)
                    aggregate |= cell.Possibles;
            }

            return aggregate;
        }


        private BitField AggregateCubeRowPossibles(int cubex, int cubey, int row)
        {
            BitField aggregate = new BitField();

            foreach (Cell cell in Cells.CubeRow(cubex, cubey, row))
            {
                if (!cell.HasValue)
                    aggregate |= cell.Possibles;
            }

            return aggregate;
        }


        private void UpdateCubeColumnDirections(int cubex, int cubey, int column, BitField columnA, BitField columnB)
        {
            foreach (Cell cell in Cells.CubeColumn(cubex, cubey, column))
            {
                // possible values that are exclusive to this column
                if (!cell.HasValue)
                    cell.VerticalDirections = cell.Possibles & !(columnA | columnB);
            }
        }


        private void UpdateCubeRowDirections(int cubex, int cubey, int row, BitField rowA, BitField rowB)
        {
            foreach (Cell cell in Cells.CubeRow(cubex, cubey, row))
            {
                // possible values that are exclusive to this row
                if (!cell.HasValue)
                    cell.HorizontalDirections = cell.Possibles & !(rowA | rowB);
            }
        }




        private bool RowDirectionElimination(Stack<Cell> cellsToUpdate)
        {
            bool modelUpdated = false;

            for (int y = 0; y < 9; y++)  // for each row/column
            {
                for (int cube = 0; cube < 3; cube++)  // for each cube
                {
                    BitField directions = new BitField();

                    foreach (Cell cell in Cells.CubeRow(cube, y / 3, y % 3)) // cells in the cube
                    {
                        if (!cell.HasValue) // aggregate the directions
                        {
                            if (!Cells.Rotated)
                                directions |= cell.HorizontalDirections;
                            else
                                directions |= cell.VerticalDirections;
                        }
                    }

                    if (!directions.IsEmpty) // update the cells that are not in the cube
                    {
                        BitField mask = !directions;

                        foreach (Cell cell in Cells.RowMinus(cube, y))
                        {
                            if (!cell.HasValue)
                            {
                                BitField temp = cell.Possibles & mask;

                                if (!temp.IsEmpty && (temp != cell.Possibles))
                                {
                                    modelUpdated = true;
                                    cell.Possibles = temp;

                                    if (cell.Possibles.Count == 1)
                                        cellsToUpdate.Push(cell);
                                }
                            }
                        }

                        directions.Reset(false);  // for next loop
                    }
                }
            }

            return modelUpdated;
        }




        private void CheckForBothRowColumnDirections(Stack<Cell> cellsToUpdate)
        {
            foreach (Cell cell in Cells)
            {
                if (!cell.HasValue)
                {
                    BitField temp = cell.HorizontalDirections & cell.VerticalDirections;

                    if (!temp.IsEmpty && (cell.Possibles != temp))
                    {
                        Debug.Assert(temp.Count == 1);
                        cell.Possibles = temp;
                        cellsToUpdate.Push(cell);
                    }
                }
            }
        }



        private bool ColumnDirectionElimination(Stack<Cell> cellsToUpdate)
        {
            Cells.Rotated = true;
            bool modelUpdated = RowDirectionElimination(cellsToUpdate);
            Cells.Rotated = false;

            return modelUpdated;
        }



        private bool DirectionElimination(Stack<Cell> cellsToUpdate)
        {
            bool modelUpdated = RowDirectionElimination(cellsToUpdate);
            modelUpdated |= ColumnDirectionElimination(cellsToUpdate);

            CheckForBothRowColumnDirections(cellsToUpdate);

            return modelUpdated;
        }




        private void CheckForSinglePossibleRow(Stack<Cell> cellsToUpdate)
        {
            var temp = new (int count, Cell cell)[10];

            for (int y = 0; y < 9; y++)  // for each row
            {

                foreach (Cell cell in Cells.Row(y))
                {
                    if (!cell.HasValue)
                    {
                        for (int index = 1; index < 10; index++) // cell values range from 1 to 9
                        {
                            if (cell.Possibles[index])
                            {
                                if (temp[index].count == 0)
                                {
                                    temp[index].count = 1;
                                    temp[index].cell = cell;
                                }
                                else
                                    temp[index].count += 1;
                            }
                        }
                    }
                }

                for (int index = 1; index < 10; index++)
                {
                    if (temp[index].count == 1)  // must be the only possible value in the row/column
                    {
                        Cell cell = temp[index].cell;

                        if (cell.Possibles.Count > 1)  // only push once
                        {
                            cell.Possibles.Reset(false);
                            cell.Possibles[index] = true;

                            cellsToUpdate.Push(cell);
                        }
                    }

                    temp[index].count = 0; // reset for next loop
                }
            }
        }



        private void CheckForSinglePossibleColumn(Stack<Cell> cellsToUpdate)
        {
            Cells.Rotated = true;
            CheckForSinglePossibleRow(cellsToUpdate);
            Cells.Rotated = false;
        }


        private void CheckForSinglePossibles(Stack<Cell> cellsToUpdate)
        {
            CheckForSinglePossibleRow(cellsToUpdate);
            CheckForSinglePossibleColumn(cellsToUpdate);
        }



        private enum Pattern { None, Lower2, Upper2, TopAndBottom };


        private Pattern FindVerticalPattern(bool a, bool b, bool c)
        {
            if (a && b && !c)
                return Pattern.Upper2;

            if (!a && b && c)
                return Pattern.Lower2;

            if (a && !b && c)
                return Pattern.TopAndBottom;

            return Pattern.None;
        }


        private bool CubeRowPatternMatchElimination(Stack<Cell> cellsToUpdate)
        {
            bool modelChanged = false;

            for (int cubey = 0; cubey < 3; cubey++)  // horizontal rows of 3 cubes
            {
                BitField temp = new BitField(true);
                BitField[,] possibles = new BitField[3, 3];

                for (int cubex = 0; cubex < 3; cubex++) //  cube in row
                {
                    for (int row = 0; row < 3; row++) // rows within the cube
                    {
                        foreach (Cell cell in Cells.CubeRow(cubex, cubey, row))
                        {
                            if (cell.HasValue)
                                temp[cell.Value] = false;
                            else if (cell.Possibles.Count == 1)
                                temp[cell.Possibles.First] = false;
                            else
                                possibles[cubex, row] |= cell.Possibles;
                        }
                    }
                }

                if (!temp.IsEmpty)   // there are candidates for pattern matching (a value isn't in any of the cells in the cube row)
                {
                    for (int cellValue = 1; cellValue < 10; cellValue++)
                    {
                        if (temp[cellValue]) // attempt to find pattern for this value
                        {
                            List<Pattern> patterns = new List<Pattern>(3)
                            {
                                // array index is [cube, row]
                                FindVerticalPattern(possibles[0, 0][cellValue], possibles[0, 1][cellValue], possibles[0, 2][cellValue]),
                                FindVerticalPattern(possibles[1, 0][cellValue], possibles[1, 1][cellValue], possibles[1, 2][cellValue]),
                                FindVerticalPattern(possibles[2, 0][cellValue], possibles[2, 1][cellValue], possibles[2, 2][cellValue])
                            };

                            bool foundPattern = false;
                            int cubex = 0;
                            int skipRow = 0;

                            if (patterns.CountOf(Pattern.Lower2) == 2)
                            {
                                // remove the cell value from possibles of lower 2 rows of the other cube
                                foundPattern = true;
                                cubex = patterns.FindIndex(item => item != Pattern.Lower2);
                                skipRow = 0;
                            }
                            else if (patterns.CountOf(Pattern.Upper2) == 2)
                            {
                                // remove the cell value from possibles of upper 2 rows of the other cube
                                foundPattern = true;
                                cubex = patterns.FindIndex(item => item != Pattern.Upper2);
                                skipRow = 2;
                            }
                            else if (patterns.CountOf(Pattern.TopAndBottom) == 2)
                            {
                                // remove the cell value from possibles of the top and bottom rows of the other cube
                                foundPattern = true;
                                cubex = patterns.FindIndex(item => item != Pattern.TopAndBottom);
                                skipRow = 1;
                            }

                            if (foundPattern)  // update the cell possibles in the other cube
                            {
                                for (int row = 0; row < 3; row++) // rows within the cube
                                {
                                    if ((row != skipRow) && possibles[cubex, row][cellValue])
                                    {
                                        // there is a possible value in this row
                                        foreach (Cell cell in Cells.CubeRow(cubex, cubey, row))
                                        {
                                            if (!cell.HasValue && cell.Possibles[cellValue] && (cell.Possibles.Count > 1))
                                            {
                                                cell.Possibles[cellValue] = false;
                                                modelChanged = true;

                                                if (cell.Possibles.Count == 1)
                                                    cellsToUpdate.Push(cell);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return modelChanged;
        }



        private bool CubeColumnPatternMatchElimination(Stack<Cell> cellsToUpdate)
        {
            Cells.Rotated = true;
            bool modelUpdated = CubeRowPatternMatchElimination(cellsToUpdate);
            Cells.Rotated = false;

            return modelUpdated;
        }



        private bool CubePatternMatchElimination(Stack<Cell> cellsToUpdate)
        {
            bool modelUpdated = CubeRowPatternMatchElimination(cellsToUpdate);
            modelUpdated |= CubeColumnPatternMatchElimination(cellsToUpdate);

            return modelUpdated;
        }


        private void DebugValidation()
        {
#if DEBUG
            foreach (Cell cell in Cells)
            {
                if (!cell.HasValue && cell.Possibles.Count < 1)
                    Debug.Fail("validation fail");
            }
#endif
        }



        private void CalculatePossibleValues()
        {
            Stack<Cell> cellsToUpdate = new Stack<Cell>();

            foreach (Cell cell in Cells)
            {
                if (cell.HasValue)
                    cellsToUpdate.Push(cell);
                else
                    cell.Possibles.Reset(true);  // directions will be recalculated
            }

            do
            {
                while (cellsToUpdate.Count > 0)
                    SimpleEliminationForCell(cellsToUpdate.Pop(), cellsToUpdate);

                bool modelChanged;

                do
                {
                    UpdateCubesDirections();

                    modelChanged = DirectionElimination(cellsToUpdate);

                    CheckForSinglePossibles(cellsToUpdate);

                    modelChanged |= CubePatternMatchElimination(cellsToUpdate);
                }
                while ((cellsToUpdate.Count == 0) && modelChanged);

            }
            while (cellsToUpdate.Count > 0);
        }
    }
}
