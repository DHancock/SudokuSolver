namespace SudokuSolver.Views;

internal sealed class ConfirmSaveDialog : ContentDialog
{
    public ConfirmSaveDialog(string puzzleName, XamlRoot xamlRoot, ElementTheme actualTheme) : base()
    {
        // for entrance transition animation
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

        XamlRoot = xamlRoot;
        RequestedTheme = actualTheme;

        Title = App.Instance.AppDisplayName;
        PrimaryButtonText = App.Instance.ResourceLoader.GetString("SaveButton");
        SecondaryButtonText = App.Instance.ResourceLoader.GetString("DontSaveButton");
        CloseButtonText = App.Instance.ResourceLoader.GetString("CancelButton");

        string template = App.Instance.ResourceLoader.GetString("ConfirmSaveTemplate");
        Content = string.Format(template, puzzleName);

        DefaultButton = ContentDialogButton.Primary;
    }
}
