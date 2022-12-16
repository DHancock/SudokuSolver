using Sudoku.Utilities;

namespace Sudoku.Views
{
    internal sealed class ErrorDialog : ContentDialog
    {
        public ErrorDialog(string message, string details, XamlRoot xamlRoot, ElementTheme actualTheme) : base()
        {
            XamlRoot = xamlRoot;
            RequestedTheme = actualTheme;
            Title = App.cDisplayName;
            PrimaryButtonText = "OK";
            Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";

            Utils.PlayExclamation();
        }
    }
}
