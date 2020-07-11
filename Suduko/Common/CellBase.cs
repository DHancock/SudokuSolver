using System;

namespace Sudoku.Common
{

    internal abstract class CellBase : IEquatable<CellBase>
    {
        public virtual int Value { get; set; } = 0;
        public int Index { get; protected set; }

        public BitField Possibles = new BitField(true);

        public BitField HorizontalDirections = new BitField();

        public BitField VerticalDirections = new BitField();



        public CellBase(int index)
        {
            Index = index;
        }


        public bool HasValue => Value > 0;


        public bool Equals(CellBase other)
        {
            if (other is null)
                return false;

            if (HasValue)
            {
                if (other.HasValue)
                    return Value == other.Value;

                return false;
            }

            return (Possibles == other.Possibles) &&
                    (VerticalDirections == other.VerticalDirections) &&
                    (HorizontalDirections == other.HorizontalDirections);
        }
    }
}
