using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class ErrorDialog : ContentDialog
{
    private ErrorDialog(FrameworkElement parent, string message, string details) : base()
    {
        // for entrance transition animation
        Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];

        XamlRoot = parent.XamlRoot;
        RequestedTheme = parent.ActualTheme;

        Title = App.cAppDisplayName;
        PrimaryButtonText = App.Instance.ResourceLoader.GetString("OKButton");

        DefaultButton = ContentDialogButton.Primary;

        Loaded += (s, e) =>
        {
            Utils.PlayExclamation();
            Content = $"{message}{Environment.NewLine}{Environment.NewLine}{details}";
        };
    }

    public static ErrorDialog Factory(FrameworkElement parent, string message, string details)
    {
        return new ErrorDialog(parent, message, details);
    }
}
