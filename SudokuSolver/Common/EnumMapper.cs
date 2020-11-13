using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Sudoku.Common
{
    /// <summary>
    /// A mapper for converting between enum member names and their associated values
    /// and vice versa. This could be useful if your enum type contains a small number
    /// of values and you need to convert formats regularly.
    ///     
    /// If the enum type has two or more members with the same value (violating design
    /// rule CA1069) the name returned for that value will be one of the possible names 
    /// but which name actually returned will be indeterminate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class EnumMapper<T> where T : struct, Enum
    {
        private sealed class Mapper
        {
            public readonly Dictionary<string, T> valueLookUp = new();
            public readonly Dictionary<T, string> nameLookUp = new();

            public Mapper()
            {
                string[] names = Enum.GetNames<T>();
                T[] values = Enum.GetValues<T>();

                for (int index = 0; index < names.Length; index++)
                {
                    string name = names[index] ;
                    T value = values[index];

                    valueLookUp.Add(name, value);

                    if ((index == 0) || (!value.Equals(values[index - 1])))
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

        public T Parse(string? src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            return data.Value.valueLookUp[src];
        }

        public bool TryParse(string? src, out T value)
        {
            if (src == null)
            {
                value = default;
                return false;
            }

            return data.Value.valueLookUp.TryGetValue(src, out value);
        }
    }
}