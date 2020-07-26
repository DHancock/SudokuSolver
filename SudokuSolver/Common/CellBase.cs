using System;

namespace Sudoku.Common
{

    internal abstract class CellBase : IEquatable<CellBase>
    {
        private int cellValue;

        public int Index { get; protected set; }

        public BitField Possibles = new BitField(true);

        public BitField HorizontalDirections = new BitField();

        public BitField VerticalDirections = new BitField();

        public Origins Origin { get; set; } = Origins.NotDefined;



        public CellBase(int index)
        {
            Index = index;
        }



        public CellBase(CellBase source)
        {
            CopyFrom(source, source.Index);
        }


        

        public virtual int Value
        {
            get { return cellValue; }
            set { cellValue = value; }
        }



        public bool HasValue => Value > 0;


        public void CopyFrom(CellBase source, int newIndex)
        {
            Index = newIndex;
            Origin = source.Origin;

            if (source.HasValue)
                cellValue = source.Value;  
            else
            {
                cellValue = 0;
                Possibles = source.Possibles;
                VerticalDirections = source.VerticalDirections;
                HorizontalDirections = source.HorizontalDirections;
            }
        }


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
