namespace Sudoku.Utils;

internal static class Dialogs
{
    public async static void ShowModalMessage(string heading, string message, XamlRoot xamlRoot)
    {
        ContentDialog messageDialog = new ContentDialog()
        {
            XamlRoot = xamlRoot,
            Title = heading,
            Content = message,
            PrimaryButtonText = "OK"
        };

        await messageDialog.ShowAsync();
    }
}