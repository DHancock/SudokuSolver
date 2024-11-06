namespace SudokuSolver.Views;

internal sealed partial class ConfirmSaveDialog : ContentDialog
{
    private ConfirmSaveDialog(FrameworkElement parent, string puzzleName) : base()
    {
        // for entrance transition animation
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

        XamlRoot = parent.XamlRoot;
        RequestedTheme = parent.ActualTheme;

        Title = App.cAppDisplayName;
        PrimaryButtonText = App.Instance.ResourceLoader.GetString("SaveButton");
        SecondaryButtonText = App.Instance.ResourceLoader.GetString("DontSaveButton");
        CloseButtonText = App.Instance.ResourceLoader.GetString("CancelButton");

        string template = App.Instance.ResourceLoader.GetString("ConfirmSaveTemplate");
        Content = string.Format(template, puzzleName);

        DefaultButton = ContentDialogButton.Primary;
    }

    public static ConfirmSaveDialog Factory(FrameworkElement parent, string message, string _)
    {
        return new ConfirmSaveDialog(parent, message);
    }
}
