
using System.Diagnostics;

using Sudoku.Common;

namespace Sudoku.Models
{
    [DebuggerDisplay("value = {Value}, index = {Index}")]
    internal sealed class Cell : CellBase
    {
        public Cell(int index) : base(index)
        {
        }
    }
}
