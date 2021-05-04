using System;
using System.Diagnostics;

using Sudoku.Common;

#nullable enable

namespace Sudoku.Models
{

    [DebuggerDisplay("[{Index  % 9}, {Index / 9}] value = {Value}, origin = {Origin}")]
    internal sealed class Cell : CellBase, IEquatable<Cell>
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

        public bool Equals(Cell? other)
        {
            return base.Equals(other);
        }

        public override bool Equals(object? obj) => Equals(obj as Cell);

        public override int GetHashCode() => throw new NotImplementedException();
    }
}
