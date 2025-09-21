using SudokuSolver.Utilities;

namespace SudokuSolver.ViewModels;

internal sealed partial class CellList : ObservableArray<Cell>
{
    private const int cLength = 81;

    public CellList() : base(cLength)
    {
        for (int index = 0; index < cLength; index++)
        {
            this[index] = new Cell(index);
        }
    }
}
