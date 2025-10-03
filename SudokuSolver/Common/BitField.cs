namespace SudokuSolver.Common;

internal struct BitField : IEquatable<BitField>
{
    private const nuint cSpan = 0b_0000_0011_1111_1110;

    private nuint data;

    public static readonly BitField AllTrue = new BitField(cSpan);
    public static readonly BitField Empty = default;

    private BitField(nuint value)
    {
        data = value;
    }

    // In a classic BitField implementation the indexer is a mask allowing
    // multiple bits to be set or tested. Here it refers to the bit number
    // of an individual bit, more like an array indexer.
    public bool this[int bit]
    {
        readonly get
        {
            Debug.Assert((cSpan | ((nuint)1 << bit)) == cSpan);

            return (data & ((nuint)1 << bit)) > 0;
        }

        set
        {
            Debug.Assert((cSpan | ((nuint)1 << bit)) == cSpan);

            if (value)
            {
                data |= (nuint)1 << bit;
            }
            else
            {
                data &= ~((nuint)1 << bit);
            }
        }
    }

    public readonly bool IsEmpty => data == 0;

    public readonly int First => (data > 0) ? BitOperations.TrailingZeroCount(data) : -1;

    public readonly int Count => BitOperations.PopCount(data);

    public readonly bool Equals(BitField other)
    {
        return data == other.data;
    }

    public static bool operator ==(BitField a, BitField b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(BitField a, BitField b)
    {
        return !(a == b);
    }

    public static BitField operator &(BitField a, BitField b)
    {
        return new BitField(a.data & b.data);
    }

    public static BitField operator |(BitField a, BitField b)
    {
        return new BitField(a.data | b.data);
    }

    public static BitField operator ~(BitField a)
    {
        return new BitField(~a.data & cSpan);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is BitField field && Equals(field);
    }

    public override readonly int GetHashCode() => (int)data;

    public override readonly string? ToString() 
    {
        StringBuilder sb = new StringBuilder(9);

        for(int index = 9; index >= 1; index--)
        {
            if (this[index])
            {
                sb.Append(index);
            }
            else
            {
                sb.Append('-');
            }
        }

        return sb.ToString();
    }
}
