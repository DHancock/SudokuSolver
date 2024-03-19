using SudokuSolver.Utilities;

namespace SudokuSolver.Views
{
    internal sealed class ErrorDialog : ContentDialog
    {
        public ErrorDialog(string message, string details, XamlRoot xamlRoot, ElementTheme actualTheme) : base()
        {
            // for entrance transition animation
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];

            XamlRoot = xamlRoot;
            RequestedTheme = actualTheme;
            Title = App.cDisplayName;
            PrimaryButtonText = "OK";
            DefaultButton = ContentDialogButton.Primary;

            Loaded += (s, e) =>
            {
                Utils.PlayExclamation();
                Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";
            };
        }
    }
}
