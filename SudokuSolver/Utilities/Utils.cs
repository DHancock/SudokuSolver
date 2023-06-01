namespace SudokuSolver.Utilities;

internal static class Utils
{
    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }

    public static int Clamp2DHorizontalIndex(int index, int total)
    {
        int remainder = index % total;

        if (index < 0)
            return (remainder == 0) ? 0 : total + remainder;

        return remainder;
    }

    public static int Clamp2DVerticalIndex(int index, int cellsInRow, int total)
    {
        if (index < 0) // moving up from the top row, select the last index in the next column to the right
            return index == -1 ? total - cellsInRow : (total + index + 1);

        if (index >= total) // moving down from the bottom row, select the first index in the next column to the left
            return index == total ? cellsInRow - 1 : (index - total - 1);

        return index;
    }
}
