namespace SudokuSolver.Common;

[DebuggerDisplay("{GetDebugStr(), nq}")]
internal struct BitField : IEquatable<BitField>
{
    private const nuint cSpan = 0b_0000_0011_1111_1110;

    private nuint value;

    public static readonly BitField AllTrue = new BitField(cSpan);
    public static readonly BitField Empty = default;

    private BitField(nuint value)
    {
        this.value = value;
    }

    public bool this[int index]
    {
        readonly get
        {
            Debug.Assert((cSpan | ((nuint)1 << index)) == cSpan);

            return (value & ((nuint)1 << index)) > 0;
        }

        set
        {
            Debug.Assert((cSpan | ((nuint)1 << index)) == cSpan);

            if (value)
            {
                this.value |= (nuint)1 << index;
            }
            else
            {
                this.value &= ~((nuint)1 << index);
            }
        }
    }

    public readonly bool IsEmpty => value == 0;

    public readonly int First => (value > 0) ? BitOperations.TrailingZeroCount(value) : -1;

    public readonly int Count => BitOperations.PopCount(value);

    public readonly bool Equals(BitField other) => value == other.value;

    public static bool operator ==(BitField a, BitField b) => a.Equals(b);

    public static bool operator !=(BitField a, BitField b) => !(a == b);

    public static BitField operator &(BitField a, BitField b) => new BitField(a.value & b.value);

    public static BitField operator |(BitField a, BitField b) => new BitField(a.value | b.value);

    public static BitField operator ~(BitField a) => new BitField(~a.value & cSpan);

    public override readonly bool Equals(object? obj) => obj is BitField field && Equals(field);

    public override readonly int GetHashCode() => (int)value;

    public readonly bool TryFormat(Span<char> span, out int charsWritten) => value.TryFormat(span, out charsWritten);

    public static bool TryParse(ReadOnlySpan<char> span, out BitField result) => nuint.TryParse(span, out result.value) && ((result.value | cSpan) == cSpan);

    public readonly string GetDebugStr() 
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
