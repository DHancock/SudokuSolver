

namespace Sudoku.Common
{
    public enum Origins { NotDefined, User, Calculated, Trial }


    internal static class OriginsMapper
    {
        private static readonly EnumMapper<Origins> mapper = new EnumMapper<Origins>();

        public static string ToName(Origins src) => mapper.ToName(src);

        public static bool TryGetValue(string src, out Origins origin) => mapper.TryGetValue(src, out origin);
    }

}

