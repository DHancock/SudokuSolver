using Sudoku.Utils;

namespace Sudoku.Views;

public sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialog(string title, string message, XamlRoot xamlRoot, ElementTheme requestedTheme)
    {
        this.InitializeComponent();

        XamlRoot = xamlRoot;
        RequestedTheme = requestedTheme;
        this.title.Text = title;
        this.message.Text = message;

        User32Sound.PlayExclamation();

        Loaded += async (s, e) =>
        {
            errorImage.Source = await MainWindow.LoadEmbeddedImageResource("Sudoku.Resources.error.png");
        };
    }
}
