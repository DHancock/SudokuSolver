namespace SudokuSolver.Views;

internal class ContentDialogHelper
{
    private ContentDialog? currentDialog = null;
    private PuzzleTabViewItem? parentTab = null;
    private ContentDialog? previousDialog = null;

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
        if (previousDialog is not null)
        {
            // while this condition may not be currently possible, it shouldn't be a fatal error either.
            Debug.Fail("Requesting too many content dialogs");
            return ContentDialogResult.None;
        }

        previousDialog = currentDialog;

        currentDialog = f(parent, message, details); 
        
        AddOpenClosedEventHandlers(currentDialog);
        previousDialog?.Hide();

        while (previousDialog is not null)
        {
            // Hide() is also asynchronous
            // Please don't try this at home kids. I'm a trained professional.
            await Task.Delay(10);
        }

        parentTab = parent;
        return await currentDialog.ShowAsync();
    }

    private void AddOpenClosedEventHandlers(ContentDialog dialog)
    {
        dialog.Opened += ContentDialog_Opened;
        dialog.Closed += ContentDialog_Closed;

        void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            // workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/5739
            // focus can escape a content dialog when access keys are shown via the alt key...
            parentTab?.AdjustMenuAccessKeys(enable: false);
        }

        void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            parentTab?.AdjustMenuAccessKeys(enable: true);

            if (previousDialog is not null) // waiting for the previous to close first before opening a second
            {
                previousDialog = null;
            }
            else 
            {
                currentDialog = null;
            }
        }
    }

    public bool IsContentDialogOpen => currentDialog is not null;

    public bool IsConfirmSaveDialogOpen => currentDialog is ConfirmSaveDialog;

    public FileOpenErrorDialog? GetFileOpenErrorDialog() => currentDialog as FileOpenErrorDialog;
}
