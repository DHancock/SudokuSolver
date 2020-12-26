using System;
using System.Collections.Generic;

#nullable enable

namespace Sudoku.Common
{
    internal static class Extensions
    {
        public static int CountOf<T>(this IEnumerable<T> list, T target) where T : notnull
        {
            return list.CountOf(i => i.Equals(target)); 
        }

        public static int CountOf<T>(this IEnumerable<T> list, Func<T, bool> match) where T : notnull
        {
            int count = 0;

            foreach (T item in list)  // assumes the list isn't sorted
            {
                if (match(item))
                    ++count;
            }

            return count;
        }
    }
}
