namespace SudokuSolver.Views;

internal sealed partial class ConfirmSaveDialog : ContentDialog
{
    public ConfirmSaveDialog(string puzzleName) : base()
    {
        PrimaryButtonText = App.Instance.ResourceLoader.GetString("SaveButton");
        SecondaryButtonText = App.Instance.ResourceLoader.GetString("DontSaveButton");
        CloseButtonText = App.Instance.ResourceLoader.GetString("CancelButton");

        string template = App.Instance.ResourceLoader.GetString("ConfirmSaveTemplate");
        Content = string.Format(template, puzzleName);

        DefaultButton = ContentDialogButton.Primary;
    }
}
