using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialog(FrameworkElement parent, string message, string details) : base()
    {
        PrimaryButtonText = App.Instance.ResourceLoader.GetString("OKButton");
        DefaultButton = ContentDialogButton.Primary;

        Loaded += (s, e) =>
        {
            Utils.PlayExclamation();
            Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";
        };
    }
}
