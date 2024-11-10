namespace SudokuSolver.Views;

internal class ContentDialogHelper
{
    private ContentDialog? currentDialog = null;
    private PuzzleTabViewItem? parentTab = null;

    public async Task<ContentDialogResult> ShowFileOpenErrorDialogAsync(PuzzleTabViewItem parent, string message, string details)
    {
        return await ShowDialogAsync(parent, FileOpenErrorDialog.Factory, message, details);
    }

    public async Task<ContentDialogResult> ShowErrorDialogAsync(PuzzleTabViewItem parent, string message, string details)
    {
        return await ShowDialogAsync(parent, ErrorDialog.Factory, message, details);
    }

    public async Task<ContentDialogResult> ShowConfirmSaveDialogAsync(PuzzleTabViewItem parent, string path)
    {
        return await ShowDialogAsync(parent, ConfirmSaveDialog.Factory, path, string.Empty);
    }

    private async Task<ContentDialogResult> ShowDialogAsync(PuzzleTabViewItem parent, Func<FrameworkElement, string, string, ContentDialog> f, string message, string details)
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
            currentDialog = f(parent, message, details);

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
