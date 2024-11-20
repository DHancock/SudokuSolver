namespace SudokuSolver.Views;

internal class ContentDialogHelper
{
    private ContentDialog? currentDialog = null;
    private PuzzleTabViewItem? parentTab = null;

    public async Task<ContentDialogResult> ShowFileOpenErrorDialogAsync(PuzzleTabViewItem parent, string message, string details)
    {
        return await ShowDialogAsync(parent, new FileOpenErrorDialog(parent, message, details));
    }

    public async Task<ContentDialogResult> ShowErrorDialogAsync(PuzzleTabViewItem parent, string message, string details)
    {
        return await ShowDialogAsync(parent, new ErrorDialog(parent, message, details));
    }

    public async Task<ContentDialogResult> ShowConfirmSaveDialogAsync(PuzzleTabViewItem parent, string path)
    {
        return await ShowDialogAsync(parent, new ConfirmSaveDialog(parent, path));
    }

    public async Task<(ContentDialogResult, string)> ShowRenameTabDialogAsync(PuzzleTabViewItem parent, string existingName)
    {
        RenameTabDialog dialog = new RenameTabDialog(parent, existingName);
        ContentDialogResult result = await ShowDialogAsync(parent, dialog);
        return (result, dialog.NewName);
    }

    public async Task<ContentDialogResult> ShowDialogAsync(PuzzleTabViewItem parent, ContentDialog dialog)
    {
        if (currentDialog is not null)
        {
            // while this may not be currently possible, it shouldn't be a fatal error either.
            Debug.Fail("canceling request for a second content dialog");
            return ContentDialogResult.None;
        }

        parentTab = parent;

        try
        {
            currentDialog = dialog;

            currentDialog.Closing += ContentDialog_Closing;
            currentDialog.Closed += ContentDialog_Closed;

            // workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/5739
            // focus can escape a content dialog when access keys are shown via the alt key...
            // (it makes no difference if the content dialog itself has any access keys)
            parentTab.AdjustMenuAccessKeys(enable: false);

            return await currentDialog.ShowAsync();
        }
        catch (Exception ex) 
        {
            Debug.Fail(ex.ToString());

            currentDialog = null;
            parentTab.AdjustMenuAccessKeys(enable: true);
            return ContentDialogResult.None;
        }
    }

    private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        parentTab?.AdjustMenuAccessKeys(enable: true);
    }

    private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        currentDialog = null;
    }
                                                       
    public bool IsContentDialogOpen => currentDialog is not null;

    public FileOpenErrorDialog? GetFileOpenErrorDialog() => currentDialog as FileOpenErrorDialog;
}
