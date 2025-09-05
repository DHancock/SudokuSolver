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
        {
            return (remainder == 0) ? 0 : total + remainder;
        }

        return remainder;
    }

    public static int Clamp2DVerticalIndex(int newIndex, int itemsInRow, int total)
    {
        if (newIndex < 0) // moving up from the top row, select the last index in the next column to the left
        {
            return newIndex == -itemsInRow ? total - 1 : (total + newIndex - 1);
        }

        if (newIndex >= total) // moving down from the bottom row, select the first index in the next column to the right
        {
            return newIndex == total + itemsInRow - 1 ? 0 : newIndex - total + 1;
        }

        return newIndex;
    }

    public static ResourceDictionary? GetThemeDictionary(string themeKey)
    {
        Debug.Assert(App.Instance.Resources.MergedDictionaries.Count == 2);
        Debug.Assert(App.Instance.Resources.MergedDictionaries[1].ThemeDictionaries.ContainsKey(themeKey));

        return App.Instance.Resources.MergedDictionaries[1].ThemeDictionaries[themeKey] as ResourceDictionary;
    }

    public static ElementTheme NormaliseTheme(ElementTheme theme)
    {
        if (theme == ElementTheme.Default)
        {
            return App.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
        }

        return theme;
    }

    public static Point GetOffsetFromXamlRoot(UIElement e)
    {
        GeneralTransform gt = e.TransformToVisual(e.XamlRoot.Content);
        return gt.TransformPoint(new Point(0f, 0f));
    }

    public static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        return new RectInt32(Convert.ToInt32(location.X * scale),
                             Convert.ToInt32(location.Y * scale),
                             Convert.ToInt32(size.X * scale),
                             Convert.ToInt32(size.Y * scale));
    }

    public static RectInt32 GetPassthroughRect(UIElement e, double topBounds = 0.0)
    {
        Point offset = GetOffsetFromXamlRoot(e);
        Vector2 actualSize = e.ActualSize;

        if (offset.Y < topBounds) // may be clipped if it's above the top edge of the scroll viewer
        {
            actualSize.Y -= (float)(topBounds - offset.Y);

            if (actualSize.Y < 0.1)
            {
                return default;
            }

            offset.Y = topBounds;
        }

        return ScaledRect(offset, actualSize, e.XamlRoot.RasterizationScale);
    }
}
