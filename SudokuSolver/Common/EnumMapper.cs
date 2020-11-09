using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Sudoku.Common
{
      /// <summary>
      /// A mapper for converting between enum field names and their associated values
      /// and visa versa. This could be useful if your enum type contains a small number
      /// of values and you need to convert formats regularly. 
      /// </summary>
      /// <typeparam name="T"></typeparam>
    internal sealed class EnumMapper<T> where T : struct, Enum
    {
        private sealed class Mapper
        {
            public Dictionary<string, T> ValueLookUp { get; } = new Dictionary<string, T>();
            public Dictionary<T, string> NameLookUp { get; } = new Dictionary<T, string>();

            public Mapper()
            {
                foreach (string name in Enum.GetNames(typeof(T)))
                {
                    T value = Enum.Parse<T>(name);

                    ValueLookUp.Add(name, value);
                    NameLookUp.Add(value, name);
                }
            }
        }

        private Lazy<Mapper> data = new Lazy<Mapper>(() => { return new Mapper(); });

        public EnumMapper()
        {
        }

        public string ToName(T src)
        {
            return data.Value.NameLookUp[src];
        }

        public bool TryGetName(T src, [NotNullWhen(returnValue: true)] out string? name)
        {
            return data.Value.NameLookUp.TryGetValue(src, out name);
        }

        public T ToValue(string src)
        {
            return data.Value.ValueLookUp[src];
        }

        public bool TryGetValue(string src, out T value)
        {                        
            return data.Value.ValueLookUp.TryGetValue(src, out value);
        }
    }
}