using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Sudoku.Common;

#nullable enable

namespace Sudoku.Models
{
    internal sealed class PuzzleModel : IEquatable<PuzzleModel>
    {
        private static class Cx
        {
            public const string Sudoku = "Sudoku";
            public const string version = "version";
            public const int current_version = 1;
            public const string Cell = "Cell";
            public const string x = "x";
            public const string y = "y";
            public const string origin = "origin";
            public const string value = "value";
        }

        public CellList Cells { get; }
        private int CompletedCellsCount { get; set; }


        public PuzzleModel()
        {
            Cells = new CellList();
            CompletedCellsCount = 0;
        }


        public PuzzleModel(PuzzleModel source)
        {
            Cells = new CellList(source.Cells);
            CompletedCellsCount = source.CompletedCellsCount;
        }


        public void Save(Stream stream)
        {
            XElement xmlTree = new XElement(Cx.Sudoku, new XAttribute(Cx.version, Cx.current_version));

            foreach (Cell cell in Cells)
            {
                if (cell.HasValue)
                {
                    xmlTree.Add(new XElement(Cx.Cell, new XElement(Cx.x, cell.Index % 9),
                                                      new XElement(Cx.y, cell.Index / 9),
                                                      new XElement(Cx.value, cell.Value),
                                                      new XElement(Cx.origin, cell.Origin.ToString())));
                }
            }

            xmlTree.Save(stream);
        }


        public void Clear()
        {
            foreach (Cell cell in Cells)
                cell.Reset();

            CompletedCellsCount = 0;
        }


        public void Open(Stream stream)
        {
            XDocument document = XDocument.Load(stream);

            // sanity check
            if ((document.Root == null) || (document.Root.Name != Cx.Sudoku))
                throw new InvalidDataException();

            // version check
            int version = 0;

            if (document.Root.HasAttributes)
            {
                XAttribute? va = document.Root.Attribute(Cx.version);

                if ((va == null) || !int.TryParse(va.Value, out version))
                    throw new InvalidDataException();
            }

            switch (version)
            {
                case 0: OpenVersion_0(document); break;
                case 1: OpenVersion_1(document); break;

                default: throw new InvalidDataException();
            }
        }


        private void OpenVersion_0(XDocument document)
        {
            foreach (XElement cell in document.Descendants(Cx.Cell))
            {
                if (int.TryParse(cell.Attribute(Cx.x)?.Value, out int x) && (x >= 0) && (x < 9)
                    && int.TryParse(cell.Attribute(Cx.y)?.Value, out int y) && (y >= 0) && (y < 9)
                    && int.TryParse(cell?.Value, out int value) && (value > 0) && (value < 10))
                {
                    int index = x + (y * 9);

                    if (ValidateNewCellValue(index, value))
                        SetCellValue(index, value, Origins.User);
                }
            }

            AttemptSimpleTrialAndError();
        }


        private void OpenVersion_1(XDocument document)
        {
            foreach (XElement cell in document.Descendants(Cx.Cell))
            {
                if (Enum.TryParse(cell.Element(Cx.origin)?.Value, out Origins o) && (o == Origins.User)
                    && int.TryParse(cell.Element(Cx.x)?.Value, out int x) && (x >= 0) && (x < 9)
                    && int.TryParse(cell.Element(Cx.y)?.Value, out int y) && (y >= 0) && (y < 9)
                    && int.TryParse(cell.Element(Cx.value)?.Value, out int value) && (value > 0) && (value < 10))
                {
                    int index = x + (y * 9);

                    if (ValidateNewCellValue(index, value))
                        SetCellValue(index, value, Origins.User);
                }
            }

            AttemptSimpleTrialAndError();
        }


        public bool Add(int index, int newValue)
        {
            if (ValidateNewCellValue(index, newValue))
            {
                SetCellValue(index, newValue, Origins.User);

                if (PuzzleIsErrorFree()) // so far, it doesn't mean the puzzle is still solvable
                {
                    AttemptSimpleTrialAndError();
                    return true;
                }

                // revert model
                SetCellValue(index, 0, Origins.NotDefined);
            }

            return false;
        }

        public bool Delete(int index, int _)
        {
            SetCellValue(index, 0, Origins.NotDefined);
            AttemptSimpleTrialAndError();
            return true;
        }


        public bool Edit(int index, int newValue)
        {
            int previousValue = Cells[index].Value;

            // delete the exiting value to recalculate the possibles
            SetCellValue(index, 0, Origins.NotDefined);

            if (Add(index, newValue))
                return true;

            // revert model
            SetCellValue(index, previousValue, Origins.User);
            AttemptSimpleTrialAndError();
            return false;
        }


        public bool EditForced(int index, int newValue)
        {
            if (ForcedCellValueIsValid(index, newValue))
            {
                ForceCellValue(index, newValue);

                if (PuzzleIsErrorFree())   // post validation...
                {
                    AttemptSimpleTrialAndError();
                    return true;
                }

                // revert the forced value, the model will be recalculated
                SetCellValue(index, 0, Origins.NotDefined);
                AttemptSimpleTrialAndError();
            }

            return false;
        }


        public bool SetOrigin(int index, int _)
        {
            Cells[index].Origin = Origins.User;
            return true;
        }


        private bool ValidateNewCellValue(int index, int newValue)
        {
            Debug.Assert(newValue > 0);

            Cell cell = Cells[index];

            if (cell.HasValue) // replacing a cell value directly isn't supported
                return false;

            return cell.Possibles[newValue];   // so far at least...
        }


        private void SetCellValue(int index, int newValue, Origins origin)
        {
            Cell cell = Cells[index];

            cell.Value = newValue;
            cell.Origin = origin;

            CalculatePossibleValues(cell, forceRecalculation: false);
        }


        private void ForceCellValue(int index, int newValue)
        {
            Cell cell = Cells[index];

            cell.Value = newValue;
            cell.Origin = Origins.User;

            CalculatePossibleValues(cell, forceRecalculation: true);
        }


        // A quick sanity check that the new forced value is valid in the current
        // puzzles context. It cannot check if the puzzle will still be solvable. 
        private bool ForcedCellValueIsValid(int index, int newValue)
        {
            foreach (Cell cell in Cells.CubeRowColumnMinus(index))
            {
                if (cell.HasValue && (cell.Origin == Origins.User) && (cell.Value == newValue))
                    return false;
            }

            return true;
        }


        private void SimpleEliminationForCell(Cell updatedCell, Stack<Cell> cellsToUpdate)
        {
            int newValue = updatedCell.Value;

            foreach (Cell cell in Cells.CubeRowColumnMinus(updatedCell.Index))
            {
                if (!cell.HasValue && cell.Possibles[newValue])
                {
                    int count = cell.Possibles.Count;

                    if (count != 1)
                    {
                        cell.Possibles[newValue] = false;

                        if (count == 2)
                            cellsToUpdate.Push(cell);  // only one possible left
                    }
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
            UpdateCubesDirections();

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
                            cell.Possibles.SetAllTo(false);
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


        private static Pattern FindVerticalPattern(bool a, bool b, bool c)
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
                                            if (!cell.HasValue && cell.Possibles[cellValue])
                                            {
                                                int count = cell.Possibles.Count;

                                                if (count != 1)
                                                {
                                                    cell.Possibles[cellValue] = false;
                                                    modelChanged = true;

                                                    if (count == 2)
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


        private bool PuzzleIsErrorFree()
        {
            if (CheckRowsAreErrorFree())
            {
                Cells.Rotated = true;
                bool errorFree = CheckRowsAreErrorFree();
                Cells.Rotated = false;

                if (errorFree)
                    return CheckCubesAreErrorFree();
            }

            return false;
        }


        private bool CheckRowsAreErrorFree()
        {
            for (int row = 0; row < 9; row++)
            {
                BitField temp = new BitField();

                foreach (Cell cell in Cells.Row(row))
                {
                    if (cell.HasValue)
                    {
                        if (temp[cell.Value])
                            return false;  // found a duplicate

                        temp[cell.Value] = true;
                    }
                }
            }

            return true;
        }

        private bool CheckCubesAreErrorFree()
        {
            for (int cube = 0; cube < 9; cube++)
            {
                BitField temp = new BitField();

                foreach (Cell cell in Cells.Cube(cube % 3, cube / 3))
                {
                    if (cell.HasValue)
                    {
                        if (temp[cell.Value])
                            return false;  // found a duplicate

                        temp[cell.Value] = true;
                    }
                }
            }
            
            return true;
        }


        private bool PuzzleIsComplete => CompletedCellsCount == Cells.Count;

        public bool PuzzleIsEmpty => CompletedCellsCount == 0;

        public bool CompletedCellCountIsValid => Cells.CountOf(cell =>
                                                    (cell.Origin == Origins.User) ||
                                                    (cell.Origin == Origins.Trial) ||
                                                    (cell.Origin == Origins.Calculated)) == CompletedCellsCount;

        private void CopyFrom(PuzzleModel other)
        {
            Cells.CopyFrom(other.Cells);
            CompletedCellsCount = other.CompletedCellsCount;
        }


#if Use_Parallel_TrialAndError

        // Use parallel tasks when attempting trial and error. 
        // The code works but I've left it disabled because:
        //
        //  0) The sequential single threaded method is fast enough.
        //
        //  1) It's quite difficult to access the performance of this code.
        //      Like all search algorithms, if the solution is found early it will be
        //      slower due to set up costs. If it's found later then it may be quicker.
        //      However the number of puzzles and their states of completion is almost 
        //      infinite. I also don't have access to different hardware.
        //
        //  2) If there is more than one solution and they are in different partitions then  
        //      which solution is found will be indeterminate. That isn't optimal, getting
        //      different results for the same action.      

        private readonly ConcurrentStack<PuzzleModel> modelCache = new();

        private void AttemptSimpleTrialAndError()
        {
            List<(int index, int value)> attempts = new(Cells.Count);
        
            foreach (Cell cell in Cells)
            {
                if (!cell.HasValue && (cell.Possibles.Count == 2))
                {
                    BitField temp = cell.Possibles;
        
                    while (!temp.IsEmpty)
                    {
                        int value = temp.First;
                        attempts.Add((cell.Index, value));
                        temp[value] = false;
                    }
                }
            }
        
            object lockObject = new();

            Parallel.ForEach(
                    attempts,
                    () =>   // task local initializer
                    {
                        if (!modelCache.TryPop(out PuzzleModel? localModel))
                            localModel = new PuzzleModel();

                        return localModel;
                    },
                    (attempt, state, localModel) =>    // body
                    {
                        // either initialise or revert changes from the previous run
                        localModel.CopyFrom(this);

                        if (!state.IsStopped)
                        {
                            localModel.SetCellValue(attempt.index, attempt.value, Origins.Trial);

                            if (!state.IsStopped && localModel.PuzzleIsComplete && localModel.PuzzleIsErrorFree())
                            {
                                if (!state.IsStopped)
                                {
                                    lock (lockObject)
                                    {
                                        if (!state.IsStopped)
                                        {
                                            state.Stop();
                                            CopyFrom(localModel);
                                        }
                                    }
                                }
                            }
                        }

                        return localModel;
                    },
                    (localModel) =>   // local finally
                    {
                        modelCache.Push(localModel);
                    });     
        }
#else

        private void AttemptSimpleTrialAndError()
        {
            PuzzleModel? originalModel = null;

            foreach (Cell cell in Cells)
            {
                if (!cell.HasValue && (cell.Possibles.Count == 2))
                {
                    if (originalModel is null)
                        originalModel = new PuzzleModel(this);

                    BitField temp = cell.Possibles;

                    while (!temp.IsEmpty)
                    {        
                        int cellValue = temp.First;
                        SetCellValue(cell.Index, cellValue, Origins.Trial);

                        if (PuzzleIsComplete && PuzzleIsErrorFree())
                            return;

                        // revert puzzle and clear the possible value
                        CopyFrom(originalModel);
                        temp[cellValue] = false;
                    }
                }
            }
        }
#endif

        private void CalculatePossibleValues(Cell updatedCell, bool forceRecalculation)
        {
            Stack<Cell> cellsToUpdate = new Stack<Cell>(Cells.Count);

            if (updatedCell.HasValue && !forceRecalculation)
                cellsToUpdate.Push(updatedCell);
            else
            {
                // it's much simpler to just start from scratch and rebuild it...
                foreach (Cell cell in Cells)
                {
                    if (cell.Origin == Origins.User)
                        cellsToUpdate.Push(cell);
                    else
                        cell.Reset(); 
                }

                CompletedCellsCount = 0;
            }

            do
            {
                while (cellsToUpdate.Count > 0)
                {
                    Cell cell = cellsToUpdate.Pop();

                    if (!cell.HasValue) // convert a single possible into a cell value
                    {
                        cell.Value = cell.Possibles.First;
                        cell.Origin = Origins.Calculated;
                    }

                    CompletedCellsCount += 1;
                    SimpleEliminationForCell(cell, cellsToUpdate);
                }

                bool modelChanged;

                do
                {
                    modelChanged = DirectionElimination(cellsToUpdate);

                    CheckForSinglePossibles(cellsToUpdate);

                    modelChanged |= CubePatternMatchElimination(cellsToUpdate);
                }
                while ((cellsToUpdate.Count == 0) && modelChanged);

            }
            while (cellsToUpdate.Count > 0);
        }

        public bool Equals(PuzzleModel? other)
        {
            if (other is null)
                return false;

            return Cells.Equals(other.Cells);
        }

        public override bool Equals(object? obj) => Equals(obj as PuzzleModel);

        public override int GetHashCode() => throw new NotImplementedException();
    }
}
