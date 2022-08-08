using Sudoku.Common;

namespace Sudoku.Models;

[DebuggerDisplay("[{Index  % 9}, {Index / 9}] value = {Value}, origin = {Origin}")]
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
