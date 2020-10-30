using System;
using System.Collections.Generic;

namespace Sudoku.Common
{
    internal static class Extensions
    {

        public static int CountOf<T>(this List<T> list, T target)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            int count = 0;

            foreach (T item in list)  // assumes the list isn't sorted
            {
                if (item is null)
                {
                    if (target is null)
                        ++count;
                }
                else if (item.Equals(target))
                    ++count;
            }

            return count;
        }
    }
}
