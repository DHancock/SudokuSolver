namespace SudokuSolver.Views;

internal sealed class ContentDialogHelper
{
    private readonly MainWindow parentWindow;
    private ContentDialog? currentDialog = null;

    public ContentDialogHelper(MainWindow window)
    {
        parentWindow = window;
    }

    public async Task<ContentDialogResult> ShowFileOpenErrorDialogAsync(TabViewItem parent, string message, string details)
    {
        return await ShowDialogAsync(parent, new FileOpenErrorDialog(message, details));
    }

    public async Task<ContentDialogResult> ShowErrorDialogAsync(TabViewItem parent, string message, string details)
    {
        return await ShowDialogAsync(parent, new ErrorDialog(message, details));
    }

    public async Task<ContentDialogResult> ShowConfirmSaveDialogAsync(TabViewItem parent, string path)
    {
        return await ShowDialogAsync(parent, new ConfirmSaveDialog(path));
    }

    public async Task<(ContentDialogResult, string)> ShowRenameTabDialogAsync(TabViewItem parent, string existingName)
    {
        RenameTabDialog dialog = new RenameTabDialog(existingName);
        ContentDialogResult result = await ShowDialogAsync(parent, dialog);
        return (result, dialog.NewName);
    }

    private async Task<ContentDialogResult> ShowDialogAsync(TabViewItem parentTab, ContentDialog dialog)
    {
        if (currentDialog is not null)
        {
            // while this may not be currently possible, it shouldn't be a fatal error either.
            Debug.Fail("canceling request for a second content dialog");
            return ContentDialogResult.None;
        }

        currentDialog = dialog;
        currentDialog.Opened += CurrentDialog_Opened;
        currentDialog.Closing += CurrentDialog_Closing;
        currentDialog.Closed += CurrentDialog_Closed;

        currentDialog.Style = (Style)Application.Current.Resources["CustomContentDialogStyle"];
        currentDialog.XamlRoot = parentTab.XamlRoot;
        currentDialog.RequestedTheme = parentTab.ActualTheme;
        currentDialog.FlowDirection = parentTab.FlowDirection;

        if (!ReferenceEquals(parentTab, parentWindow.SelectedTab))
        {
            parentWindow.SelectedTab = parentTab;
        }

        if (parentWindow.WindowState == WindowState.Minimized)
        {
            parentWindow.WindowState = WindowState.Normal;
        }

        return await currentDialog.ShowAsync();
    }

    private void CurrentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        parentWindow.ContentDialogOpened();
    }

    private void CurrentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        parentWindow.ContentDialogClosing();
    }

    private void CurrentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        sender.Opened -= CurrentDialog_Opened;
        sender.Closing -= CurrentDialog_Closing;
        sender.Closed -= CurrentDialog_Closed;

        currentDialog = null;
        parentWindow.ContentDialogClosed();
    }

    public void ThemeChanged(ElementTheme theme)
    {
        if (currentDialog is not null)  // the settings tab on another window can change the theme
        {
            currentDialog.RequestedTheme = theme;
        }
    }

    public bool IsContentDialogOpen => currentDialog is not null;

    public FileOpenErrorDialog? GetFileOpenErrorDialog() => currentDialog as FileOpenErrorDialog;
}
