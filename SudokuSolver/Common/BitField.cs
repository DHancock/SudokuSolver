using System;
using System.Diagnostics;

namespace Sudoku.Common
{

    [DebuggerTypeProxy(typeof(BitFieldDebugProxy))]
    internal struct BitField
    {
        private const uint cSpan = 0x03FE;   // cell values range from 1 to 9

        private uint data;


        public BitField(bool toSpan)
        {
            data = toSpan ? cSpan : 0U;
        }

        private BitField(uint value)
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
                Debug.Assert((bit > 0) && (bit < 10));

                return (data & (1U << bit)) > 0;
            }

            set
            {
                Debug.Assert((bit > 0) && (bit < 10));

                if (value)
                    data |= 1U << bit;
                else
                    data &= ~(1U << bit);
            }
        }



        public void SetAllTo(bool toSpan)
        {
            data = toSpan ? cSpan : 0U;
        }


        public bool IsEmpty => data == 0U;



        public int First
        {
            get
            {
                uint mask = 2;
                int index = 1;

                if (data != 0)
                {
                    while ((data & mask) == 0)
                    {
                        index++;
                        mask <<= 1;
                    }

                    return index;
                }

                return 0;
            }
        }



        public int Count
        {
            get
            {
                uint temp = data >> 1;
                int count = 0;

                while (temp != 0U)
                {
                    if ((temp & 1U) != 0)
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
            return new BitField((~a.data) & cSpan);
        }


        public override bool Equals(object? obj)
        {
            if (obj is BitField a)
                return this == a;

            return false;
        }

        public override int GetHashCode() => HashCode.Combine<uint>(data);



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
                    return string.Create(9, a, (Span<char> chars, BitField state) =>
                    {
                        for (int i = 0; i < chars.Length; i++)
                        {
                            if (state[i + 1])
                                chars[i] = (char)(i + '1');
                            else
                                chars[i] = '-';
                        }
                    });
                }
            }
        }
    }
}
