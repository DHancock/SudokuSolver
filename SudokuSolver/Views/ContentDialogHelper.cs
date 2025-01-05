using SudokuSolver.Utilities;

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
        currentDialog.Closing += ContentDialog_Closing;
        currentDialog.Closed += ContentDialog_Closed;
        currentDialog.Loaded += CurrentDialog_Loaded;

        currentDialog.Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
        currentDialog.Title = App.cAppDisplayName;
        currentDialog.XamlRoot = parentTab.XamlRoot;
        currentDialog.RequestedTheme = parentTab.ActualTheme;
        currentDialog.FlowDirection = parentTab.FlowDirection;

        if (!ReferenceEquals(parentTab, parentWindow.SelectedTab))
        {
            parentWindow.SelectedTab = parentTab;
        }

        selectedTab = (ITabItem)parentTab;

        // workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/5739
        // focus can escape a content dialog when access keys are shown via the alt key...
        // (it makes no difference if the content dialog itself has any access keys)
        selectedTab.AdjustMenuAccessKeys(enable: false);

        return await currentDialog.ShowAsync();
    }

    private static void CurrentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        ContentControl? contentControl = ((ContentDialog)sender).FindChild<ContentControl>("Title");

        if (contentControl is not null)
        {                    
            // no lightweight styling, and size 20 is a bit loud
            contentControl.FontSize = 18;
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
