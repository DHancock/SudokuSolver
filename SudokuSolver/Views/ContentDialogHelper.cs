namespace SudokuSolver.Views;

internal class ContentDialogHelper
{
    private readonly MainWindow parentWindow;

    private ContentDialog? currentDialog = null;
    private ITabItem? selectedTab = null;

    public ContentDialogHelper(MainWindow window)
    {
        parentWindow = window;
    }

    public async Task<ContentDialogResult> ShowFileOpenErrorDialogAsync(string message, string details)
    {
        return await ShowDialogAsync(new FileOpenErrorDialog(message, details));
    }

    public async Task<ContentDialogResult> ShowErrorDialogAsync(string message, string details)
    {
        return await ShowDialogAsync(new ErrorDialog(message, details));
    }

    public async Task<ContentDialogResult> ShowConfirmSaveDialogAsync(string path)
    {
        return await ShowDialogAsync(new ConfirmSaveDialog(path));
    }

    public async Task<(ContentDialogResult, string)> ShowRenameTabDialogAsync(string existingName)
    {
        RenameTabDialog dialog = new RenameTabDialog(existingName);
        ContentDialogResult result = await ShowDialogAsync(dialog);
        return (result, dialog.NewName);
    }

    public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        if (currentDialog is not null)
        {
            // while this may not be currently possible, it shouldn't be a fatal error either.
            Debug.Fail("canceling request for a second content dialog");
            return ContentDialogResult.None;
        }
          
        // this may not be the same tab whose context menu action caused the content dialog to be shown 
        selectedTab = (ITabItem)parentWindow.SelectedTab;

        try
        {
            currentDialog = dialog;

            currentDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            currentDialog.Title = App.cAppDisplayName;
            currentDialog.XamlRoot = parentWindow.Content.XamlRoot;
            currentDialog.RequestedTheme = ((FrameworkElement)parentWindow.Content).ActualTheme;

            currentDialog.Closing += ContentDialog_Closing;
            currentDialog.Closed += ContentDialog_Closed;

            // workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/5739
            // focus can escape a content dialog when access keys are shown via the alt key...
            // (it makes no difference if the content dialog itself has any access keys)
            selectedTab.AdjustMenuAccessKeys(enable: false);

            return await currentDialog.ShowAsync();
        }
        catch (Exception ex) 
        {
            Debug.Fail(ex.ToString());

            currentDialog = null;
            selectedTab.AdjustMenuAccessKeys(enable: true);
            return ContentDialogResult.None;
        }
    }

    private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        selectedTab?.AdjustMenuAccessKeys(enable: true);
    }

    private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        currentDialog = null;
    }
                                                       
    public bool IsContentDialogOpen => currentDialog is not null;

    public FileOpenErrorDialog? GetFileOpenErrorDialog() => currentDialog as FileOpenErrorDialog;
}
