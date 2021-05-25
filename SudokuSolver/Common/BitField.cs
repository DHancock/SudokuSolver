using System;
using System.Diagnostics;

#nullable enable

namespace Sudoku.Common
{

    [DebuggerTypeProxy(typeof(BitFieldDebugProxy))]
    internal struct BitField : IEquatable<BitField>
    {
        private const int cMinIndex = 1;    // cell values range from 1 to 9
        private const int cMaxIndex = 9;

        private const nuint cSpan = 0b_0000_0011_1111_1110;   

        private nuint data;

        public static readonly BitField AllTrue = new BitField(cSpan);
        public static readonly BitField Empty = new BitField(0);

        private BitField(nuint value)
        {
            data = value;
        }

        // In a classic BitField implementation the indexer is a mask allowing
        // multiple bits to be set or tested. Here it refers to the bit number
        // of an individual bit, more like an array indexer.
        public bool this[int bit]
        {
            get
            {
                Debug.Assert((bit >= cMinIndex) && (bit <= cMaxIndex));

                return (data & ((nuint)1 << bit)) > 0;
            }

            set
            {
                Debug.Assert((bit >= cMinIndex) && (bit <= cMaxIndex));

                if (value)
                    data |= (nuint)1 << bit;
                else
                    data &= ~((nuint)1 << bit);
            }
        }

        public bool IsEmpty => data == 0;

        public int First
        {
            get
            {
                if (data != 0)
                {
                    nuint mask = 2;
                    int index = 1;

                    while ((data & mask) == 0)
                    {
                        index++;
                        mask <<= 1;
                    }

                    return index;
                }

                return -1;
            }
        }

        public int Count
        {
            get
            {
                nuint temp = data >> 1;
                int count = 0;

                while (temp != 0)
                {
                    if ((temp & 1) != 0)
                        ++count;

                    temp >>= 1;
                }

                return count;
            }
        }

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

        public static BitField operator !(BitField a)
        {
            return new BitField(~a.data & cSpan);
        }

        public override bool Equals(object? obj)
        {
            if (obj is BitField a)
                return this == a;

            return false;
        }

        public bool Equals(BitField other)
        {
            return data == other.data;
        }

        public override int GetHashCode() => HashCode.Combine(data);


        private sealed class BitFieldDebugProxy
        {
            private BitField a;

            public BitFieldDebugProxy(BitField bitfield)
            {
                a = bitfield;
            }

            public string DebugView
            {
                get
                {
                    return string.Create(cMaxIndex - cMinIndex + 1, a, (Span<char> chars, BitField state) =>
                    {
                        for (int i = cMinIndex; i <= cMaxIndex; i++)
                            chars[i - cMinIndex] = state[i] ? (char)((i % 10) + '0') : '-' ;
                    });
                }
            }
        }
    }
}
