using System;
using System.Diagnostics;


namespace Sudoku.Common
{

    [DebuggerTypeProxy(typeof(BitFieldDebugProxy))]
    internal struct BitField
    {
        private const uint cSpan = 0x000003FE;   // cell values range from 1 to 9

        private uint data;


        public BitField(bool toSpan)
        {
            data = (toSpan) ? cSpan : 0U;
        }

        private BitField(uint value)
        {
            data = value;
        }



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



        public void Reset(bool toSpan)
        {
            data = (toSpan) ? cSpan : 0U;
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

                do
                {
                    if ((temp & 1U) != 0)
                        ++count;

                    temp >>= 1;
                }
                while (temp != 0);

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


        public override bool Equals(object obj)
        {
            if (obj is BitField a)
                return this == a;

            return false;
        }

        public override int GetHashCode() => HashCode.Combine<uint>(data);
    }



    internal sealed class BitFieldDebugProxy
    {
        private BitField a ;

        public BitFieldDebugProxy(BitField bitfield)
        {
            a = bitfield;
        }

        public string DebugView
        {
            get
            {
                string output = "[1..9] = ";

                for (int index = 1; index < 10; index++)
                {
                    output += a[index] ? "1" : "0";

                    if (index % 3 == 0)
                        output += " ";
                }

                return output;
            }
        }
    }
}
