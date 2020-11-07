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
    internal class EnumMapper<T> where T : struct, Enum
    {
        private readonly Dictionary<string, T> valueLookUp = new Dictionary<string, T>();
        private readonly Dictionary<T, string> nameLookUp = new Dictionary<T, string>();

        private bool initialised = false;
        private readonly object initialiseLock = new object();



        public string ToName(T src)
        {
            if (!initialised)
                Init();

            return nameLookUp[src];
        }


        public bool TryGetName(T src, [NotNullWhen(returnValue: true)] out string? name)
        {
            if (!initialised)
                Init();

            return nameLookUp.TryGetValue(src, out name);
        }


        public T ToValue(string src)
        {
            if (!initialised)
                Init();

            return valueLookUp[src];
        }


        public bool TryGetValue(string src, out T value)
        {
            if (!initialised)
                Init();
                                                    
            return valueLookUp.TryGetValue(src, out value);
        }


        private void Init()
        {
            lock(initialiseLock)
            {
                if (!initialised)
                {
                    initialised = true; 
            
                    foreach (string name in Enum.GetNames(typeof(T)))
                    {
                        T value = (T)Enum.Parse<T>(name);

                        valueLookUp.Add(name, value);
                        nameLookUp.Add(value, name);
                    }
                }
            }
        }
    }
}