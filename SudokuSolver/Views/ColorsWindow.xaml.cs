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

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(Win32Interop.GetWindowIdFromWindow(WindowPtr));

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
            CustomTitleBar.UpdateThemeAndTransparency(Settings.Data.ViewSettings.Theme);
            Activated += CustomTitleBar.ParentWindow_Activated;
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            LayoutRoot.SizeChanged += (s, a) => SetWindowDragRegions();
            LightExpander.SizeChanged += (s, a) => SetWindowDragRegions();
            DarkExpander.SizeChanged += (s, a) => SetWindowDragRegions();

            // the drag regions need to be adjusted for menu fly outs
            FileMenuItem.Loaded += (s, a) => ClearWindowDragRegions();
            ViewMenuItem.Loaded += (s, a) => ClearWindowDragRegions();
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

    private void ClearWindowDragRegions()
    {
        Debug.Assert(AppWindowTitleBar.IsCustomizationSupported());
        Debug.Assert(AppWindow.TitleBar.ExtendsContentIntoTitleBar);

        // allow mouse interaction with fly outs,  
        // including clicks anywhere in the client area used to dismiss the flyout
        inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
    }

    private void SetWindowDragRegions()
    {
        if (LightExpander.IsLoaded && DarkExpander.IsLoaded && Menu.IsLoaded)
        {
            Debug.Assert(AppWindowTitleBar.IsCustomizationSupported());
            Debug.Assert(AppWindow.TitleBar.ExtendsContentIntoTitleBar);

            double scale = LightExpander.XamlRoot.RasterizationScale;

            RectInt32 windowRect = new RectInt32(0, 0, AppWindow.ClientSize.Width, AppWindow.ClientSize.Height);
            RectInt32 menuRect = Utils.ScaledRect(Menu.ActualOffset, Menu.ActualSize, scale);
            RectInt32 lightRect = Utils.ScaledRect(CalculateOffset(LightExpander), LightExpander.ActualSize, scale);
            RectInt32 darkRect = Utils.ScaledRect(CalculateOffset(DarkExpander), DarkExpander.ActualSize, scale);

            using (SimpleRegion region = new SimpleRegion(windowRect))
            {
                region.Subtract(menuRect);
                region.Subtract(lightRect);
                region.Subtract(darkRect);

                if (MainScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                {
                    ScrollBar? sb = MainScrollViewer.FindControl<ScrollBar>("VerticalScrollBar");

                    if (sb is not null)
                    {
                        Vector3 offset = CalculateOffset(sb);
                        offset.Y = CalculateOffset(MainScrollViewer).Y;
                        region.Subtract(Utils.ScaledRect(offset, sb.ActualSize, scale));
                    }
                }

                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, region.ToArray());
            }
        }
    }

    private static Vector3 CalculateOffset(FrameworkElement source)
    {
        // ActualOffset is relative to it's parent container
        Vector3 offset = source.ActualOffset;

        while (source.Parent is FrameworkElement parent) 
        {
            source = parent;
            offset += source.ActualOffset;
        }

        return offset;
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
}
