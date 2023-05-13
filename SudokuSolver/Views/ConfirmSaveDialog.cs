namespace SudokuSolver.Views;

internal sealed class ConfirmSaveDialog : ContentDialog
{
    public ConfirmSaveDialog(string puzzleName, XamlRoot xamlRoot, ElementTheme actualTheme) : base()
    {
        // for entrance transition animation
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

        XamlRoot = xamlRoot;
        RequestedTheme = actualTheme;
        Title = App.cDisplayName;
        PrimaryButtonText = "Save";
        SecondaryButtonText = "Don't Save";
        CloseButtonText = "Cancel";
        Content = $"Would you like to save changes to {puzzleName}?";
        DefaultButton = ContentDialogButton.Primary;
    }
}
