using Sudoku.Common;

namespace Sudoku.ViewModels;

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
