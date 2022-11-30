namespace Sudoku.Views;

public sealed partial class ConfirmSaveDialog : ContentDialog
{
    public ConfirmSaveDialog(string puzzleName, XamlRoot xamlRoot, ElementTheme actualTheme)
    {
        this.InitializeComponent();

        XamlRoot = xamlRoot;
        RequestedTheme = actualTheme;
        Title = App.cDisplayName;
        Content = $"Do you want to save changes to {puzzleName}?";
        DefaultButton = ContentDialogButton.Primary;
    }
}