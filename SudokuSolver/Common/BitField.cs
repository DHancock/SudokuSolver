﻿namespace SudokuSolver.Common;

[DebuggerTypeProxy(typeof(BitFieldDebugProxy))]
internal struct BitField
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

    public static bool operator ==(BitField a, BitField b)
    {
        return a.data == b.data;
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
        if (obj is BitField a)
        {
            return this == a;
        }

        return false;
    }

    public override readonly int GetHashCode() => throw new NotImplementedException();


    private sealed class BitFieldDebugProxy
    {
        private readonly BitField a;

        public BitFieldDebugProxy(BitField bitField)
        {
            a = bitField;
        }

        public string DebugView
        {
            get
            {
                List<char> chars = new();
                nuint bits = cSpan;

                while (bits > 0)
                {
                    if ((bits & 1) > 0)
                    {
                        chars.Add(a[chars.Count] ? (char)((chars.Count % 10) + '0') : '-');
                    }
                    else
                    {
                        chars.Add('.');   // it's not in cSpan
                    }

                    bits >>= 1;
                }

                return string.Create(chars.Count, chars, (Span<char> charSpan, List<char> state) =>
                {
                    int readIndex = 0;
                    int writeIndex = state.Count;

                    while (writeIndex > 0)
                        charSpan[--writeIndex] = state[readIndex++];
                });
            }
        }
    }
}
