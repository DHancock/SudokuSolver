namespace SudokuSolver.Utilities;

internal static class Utils
{
    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }

    public static int Clamp2DHorizontalIndex(int newIndex, int total)
    {
        int remainder = newIndex % total;

        if (newIndex < 0)
            return (remainder == 0) ? 0 : total + remainder;

        return remainder;
    }

    public static int Clamp2DVerticalIndex(int newIndex, int itemsInRow, int total)
    {
        if (newIndex < 0) // moving up from the top row, select the last index in the next column to the left
            return newIndex == -itemsInRow ? total - 1 : (total + newIndex - 1);

        if (newIndex >= total) // moving down from the bottom row, select the first index in the next column to the right
            return newIndex == total + itemsInRow - 1 ? 0 : newIndex - total + 1;

        return newIndex;
    }


}
