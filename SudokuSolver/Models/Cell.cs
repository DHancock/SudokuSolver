using SudokuSolver.Common;

namespace SudokuSolver.Models;

internal sealed class Cell : CellBase
{
    public Cell(int index) : base(index)
    {
    }

    public Cell(Cell source) : base(source)                        
    {
    }

    public void Reset()
    {
        Origin = Origins.NotDefined;
        Value = 0;
        Possibles = BitField.AllTrue;
        HorizontalDirections = BitField.Empty;
        VerticalDirections = BitField.Empty;
    }
}
