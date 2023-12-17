using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class ColorsWindow : WindowBase
{
    public ColorsViewModel ViewModel { get; }

    private readonly InputNonClientPointerSource inputNonClientPointerSource;

        
    public ColorsWindow()
    {
        this.InitializeComponent();

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        ViewModel = new ColorsViewModel();

        LayoutRoot.RequestedTheme = ViewModel.Theme;
        SystemBackdrop = new MicaBackdrop();

        AppWindow.SetIcon("Resources\\app.ico");

        AppWindow.MoveAndResize(App.Instance.GetNewWindowPosition(this, Settings.Data.ColorsRestoreBounds));

        string title = App.cDisplayName + " Colors";

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            CustomTitleBar.Title = title;
            CustomTitleBar.ParentAppWindow = AppWindow;
            CustomTitleBar.UpdateThemeAndTransparency(ViewModel.Theme);
            Activated += CustomTitleBar.ParentWindow_Activated;
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            LayoutRoot.SizeChanged += (s, a) => SetWindowDragRegions();
            LightExpander.SizeChanged += (s, a) => SetWindowDragRegions();
            DarkExpander.SizeChanged += (s, a) => SetWindowDragRegions();

            // the drag regions need to be adjusted for menu fly outs
            FileMenuItem.Loaded += (s, a) => ClearWindowDragRegions();
            ViewMenuItem.Loaded += (s, a) => ClearWindowDragRegions();
            FileMenuItem.Unloaded += (s, a) => SetWindowDragRegions();
            ViewMenuItem.Unloaded += (s, a) => SetWindowDragRegions();
        }
        else
        {
            Title = title;
            CustomTitleBar.Visibility = Visibility.Collapsed;
        }

        // the app window's title is used in the task switcher
        AppWindow.Title = title;

        AppWindow.Closing += async (s, e) =>
        {
            bool isLastWindow = App.Instance.UnRegisterWindow(this);

            Settings.Data.ColorsRestoreBounds = RestoreBounds;

            AppWindow.Hide();

            if (isLastWindow)
                await Settings.Data.Save();
        };

        LayoutRoot.Loaded += (s, e) =>
        {
            DarkExpander.IsExpanded = ViewModel.IsDarkThemed;
            LightExpander.IsExpanded = !DarkExpander.IsExpanded;
        };
    }
 
    private void PickerFlyoutOpened(AssyntSoftware.WinUI3Controls.SimpleColorPicker sender, bool args)
    {
        ClearWindowDragRegions();
    }

    private void PickerFlyoutClosed(AssyntSoftware.WinUI3Controls.SimpleColorPicker sender, bool args)
    {
        SetWindowDragRegions();
    }

    private void CloseClickHandler(object sender, RoutedEventArgs e)
    {
        PostCloseMessage();
    }

    private void ExitClickHandler(object sender, RoutedEventArgs e)
    {
        App.Instance.AttemptCloseAllWindows();
    }

    private async void AboutClickHandler(object sender, RoutedEventArgs e)
    {
        AboutBox aboutBox = new AboutBox(Content.XamlRoot);
        aboutBox.RequestedTheme = LayoutRoot.ActualTheme;

        aboutBox.Closed += (s, e) => SetWindowDragRegions();

        await aboutBox.ShowAsync();
    }

    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        SetWindowDragRegions();
    }
}
