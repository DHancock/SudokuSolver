namespace Sudoku.Common;

internal abstract class CellBase : IEquatable<CellBase>
{
    private int cellValue;

    public int Index { get; protected set; }

    public BitField Possibles = BitField.AllTrue;
    public BitField HorizontalDirections = BitField.Empty;
    public BitField VerticalDirections = BitField.Empty;

    public Origins Origin { get; set; } = Origins.NotDefined;


    public CellBase(int index)
    {
        Index = index;
    }


    public CellBase(CellBase source)
    {
        CopyFrom(source);
    }
    

    public virtual int Value
    {
        get { return cellValue; }
        set { cellValue = value; }
    }


    public bool HasValue => Value > 0;


    public void CopyFrom(CellBase source)
    {
        Index = source.Index;
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

    public bool Equals(CellBase? other)
    {
        if (other is null)
            return false;

        if (HasValue == other.HasValue)
        {
            if (HasValue)
                return (Value == other.Value) && (Origin == other.Origin);

            return (Possibles == other.Possibles) &&
                    (VerticalDirections == other.VerticalDirections) &&
                    (HorizontalDirections == other.HorizontalDirections);
        }

        return false;
    }

    public override bool Equals(object? obj) => Equals(obj as CellBase);

    public override int GetHashCode() => throw new NotImplementedException();
}
