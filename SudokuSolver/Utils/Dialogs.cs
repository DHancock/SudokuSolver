namespace Sudoku.Utils;

internal static class Dialogs
{
    public async static void ShowModalMessage(string heading, string message, XamlRoot xamlRoot, ElementTheme theme)
    {
        ContentDialog messageDialog = new ContentDialog()
        {
            XamlRoot = xamlRoot,
            RequestedTheme = theme,
            Title = heading,
            Content = message,
            PrimaryButtonText = "OK"
        };

        await messageDialog.ShowAsync();
    }
}