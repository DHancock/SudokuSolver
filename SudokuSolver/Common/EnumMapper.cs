using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Sudoku.Common
{
      /// <summary>
      /// A mapper for converting between enum field names and their associated values
      /// and vice versa. This could be useful if your enum type contains a small number
      /// of values and you need to convert formats regularly. 
      /// </summary>
      /// <typeparam name="T"></typeparam>
    internal sealed class EnumMapper<T> where T : struct, Enum
    {
        private sealed class Mapper
        {
            public readonly Dictionary<string, T> valueLookUp = new Dictionary<string, T>();
            public readonly Dictionary<T, string> nameLookUp = new Dictionary<T, string>();

            public Mapper()
            {
                foreach (string name in Enum.GetNames(typeof(T)))
                {
                    T value = Enum.Parse<T>(name);

                    valueLookUp.Add(name, value);
                    nameLookUp.Add(value, name);
                }
            }
        }

        private readonly Lazy<Mapper> data = new Lazy<Mapper>(() => { return new Mapper(); });

        public EnumMapper()
        {
        }

        public string ToName(T src)
        {
            return data.Value.nameLookUp[src];
        }

        public bool TryGetName(T src, [NotNullWhen(returnValue: true)] out string? name)
        {
            return data.Value.nameLookUp.TryGetValue(src, out name);
        }

        public T ToValue(string src)
        {
            return data.Value.valueLookUp[src];
        }

        public bool TryGetValue(string src, out T value)
        {                        
            return data.Value.valueLookUp.TryGetValue(src, out value);
        }
    }
}