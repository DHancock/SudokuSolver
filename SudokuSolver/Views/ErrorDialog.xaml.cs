using Sudoku.Utilities;

namespace Sudoku.Views;

public sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialog(string message, string details, XamlRoot xamlRoot, ElementTheme actualTheme)
    {
        this.InitializeComponent();

        XamlRoot = xamlRoot;
        RequestedTheme = actualTheme;
        Title = App.cDisplayName;
        Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";

        User32Sound.PlayExclamation();
    }
}
