namespace SudokuSolver.Common;

internal abstract class CellBase : IEquatable<CellBase>
{
    public int Index;
    public int Value = 0;
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
        Index = source.Index;
        CopyFrom(source);
    }
    
    public bool HasValue => Value > 0;

    public void CopyFrom(CellBase source)
    {
        Debug.Assert(Index == source.Index);

        Origin = source.Origin;

        if (source.HasValue)
        {
            Value = source.Value;
        }
        else
        {
            Value = 0;
            Possibles = source.Possibles;
            VerticalDirections = source.VerticalDirections;
            HorizontalDirections = source.HorizontalDirections;
        }
    }

    public bool Equals(CellBase? other)
    {
        if (other is null)
        {
            return false;
        }

        Debug.Assert(Index == other.Index);

        if (HasValue == other.HasValue)
        {
            if (HasValue)
            {
                return (Value == other.Value) && (Origin == other.Origin);
            }

            return (Possibles == other.Possibles) &&
                    (VerticalDirections == other.VerticalDirections) &&
                    (HorizontalDirections == other.HorizontalDirections);
        }

        return false;
    }

    public override bool Equals(object? obj) => Equals(obj as CellBase);

    public override int GetHashCode() => throw new NotImplementedException();
}
