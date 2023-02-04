using SudokuSolver.Common;

namespace SudokuSolver.ViewModels;

internal sealed class Cell : CellBase
{
    public Cell(int index) : base(index)
    {
    }

    public Cell(Cell source) : base(source)
    {
    }

    public Cell(Models.Cell source) : base(source)
    {
    }
}
