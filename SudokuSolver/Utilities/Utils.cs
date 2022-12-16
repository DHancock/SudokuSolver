namespace Sudoku.Utilities;

internal static class Utils
{
    public static async Task<BitmapImage> LoadEmbeddedImageResource(string resourcePath)
    {
        BitmapImage bitmapImage = new BitmapImage();

        using (Stream? resourceStream = typeof(App).Assembly.GetManifestResourceStream(resourcePath))
        {
            Debug.Assert(resourceStream is not null);

            using (IRandomAccessStream stream = resourceStream.AsRandomAccessStream())
            {
                await bitmapImage.SetSourceAsync(stream);
            }
        }

        return bitmapImage;
    }

    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }
}
