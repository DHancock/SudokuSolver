using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal abstract class WindowBase : Window
{
    private enum SC
    {
        RESTORE = 0xF120,
        SIZE = 0xF000,
        MOVE = 0xF010,
        MINIMIZE = 0xF020,
        MAXIMIZE = 0xF030,
        CLOSE = 0xF060,
    }

    public double MinWidth { get; set; }
    public double MinHeight { get; set; }
    public double InitialWidth { get; set; }
    public double InitialHeight { get; set; }
    public IntPtr WindowPtr { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand MoveCommand { get; }
    public RelayCommand SizeCommand { get; }
    public RelayCommand MinimizeCommand { get; }
    public RelayCommand MaximizeCommand { get; }
    public RelayCommand CloseCommand { get; }

    private readonly InputNonClientPointerSource inputNonClientPointerSource;
    private readonly SUBCLASSPROC subClassDelegate;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private MenuFlyout? systemMenu;

    public WindowBase()
    {
        WindowPtr = WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)WindowPtr, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        AppWindow.Changed += AppWindow_Changed;
        Activated += App.Instance.RecordWindowActivated;

        RestoreCommand = new RelayCommand(o => PostSysCommandMessage(SC.RESTORE), CanRestore);
        MoveCommand = new RelayCommand(o => PostSysCommandMessage(SC.MOVE), CanMove);
        SizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.SIZE), CanSize);
        MinimizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MINIMIZE), CanMinimize);
        MaximizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MAXIMIZE), CanMaximize);
        CloseCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE));
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            if (WindowState == WindowState.Normal)
            {
                restorePosition = AppWindow.Position;
                restoreSize = AppWindow.Size;

                if (args.DidSizeChange)
                    SetWindowDragRegions();
            }
        }
        else if (args.DidPresenterChange) // including properties of the current presenter
        {
            if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                HideSystemMenu();
            else
                UpdateSystemMenuItemsEnabledState();
        }
    }

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        const int VK_SPACE = 0x0020;
        const int HTCAPTION = 0x0002;

        switch (uMsg)
        {
            case PInvoke.WM_GETMINMAXINFO:
            {
                MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                double scaleFactor = GetScaleFactor();
                minMaxInfo.ptMinTrackSize.X = Math.Max(ConvertToDeviceSize(MinWidth, scaleFactor), minMaxInfo.ptMinTrackSize.X);
                minMaxInfo.ptMinTrackSize.Y = Math.Max(ConvertToDeviceSize(MinHeight, scaleFactor), minMaxInfo.ptMinTrackSize.Y);
                Marshal.StructureToPtr(minMaxInfo, lParam, true);
                break;
            }

            case PInvoke.WM_SYSCOMMAND:
            {
                if ((lParam == VK_SPACE) && (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen))
                {
                    HideSystemMenu();

                    if (ShowSystemMenu(viaKeyboard: true))
                        return (LRESULT)0;
                }

                break;
            }

            case PInvoke.WM_NCRBUTTONUP:
            {
                if (wParam == HTCAPTION)
                {
                    HideSystemMenu();

                    if (ShowSystemMenu(viaKeyboard: false))
                        return (LRESULT)0;
                }

                break;
            }

            case PInvoke.WM_NCLBUTTONDOWN:
            {
                if (wParam == HTCAPTION)
                {
                    HideSystemMenu();
                }

                break;
            }
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void PostSysCommandMessage(SC command)
    {
        bool success = PInvoke.PostMessage((HWND)WindowPtr, PInvoke.WM_SYSCOMMAND, (WPARAM)(nuint)command, 0);
        Debug.Assert(success);
    }

    private bool ShowSystemMenu(bool viaKeyboard)
    {
        if ((systemMenu is null) && (Content is FrameworkElement root) && root.Resources.TryGetValue("SystemMenuFlyout", out object? res))
            systemMenu = res as MenuFlyout;

        if (systemMenu is not null)
        {
            System.Drawing.Point p = default;

            if (viaKeyboard || !PInvoke.GetCursorPos(out p) || !PInvoke.ScreenToClient((HWND)WindowPtr, ref p))
            {
                p.X = 3;
                p.Y = AppWindow.TitleBar.Height;
            }

            double scale = GetScaleFactor();
            systemMenu.ShowAt(null, new Windows.Foundation.Point(p.X / scale, p.Y / scale));
            return true;
        }

        return false;
    }

    private void HideSystemMenu()
    {
        if ((systemMenu is not null) && systemMenu.IsOpen)
            systemMenu.Hide();
    }

    private void UpdateSystemMenuItemsEnabledState()
    {
        RestoreCommand.RaiseCanExecuteChanged();
        MoveCommand.RaiseCanExecuteChanged();
        SizeCommand.RaiseCanExecuteChanged();
        MinimizeCommand.RaiseCanExecuteChanged();
        MaximizeCommand.RaiseCanExecuteChanged();
    }

    public void PostCloseMessage() => PostSysCommandMessage(SC.CLOSE);

    private bool CanRestore(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && (op.State == OverlappedPresenterState.Maximized);
    }

    private bool CanMove(object? param)
    {
        if (AppWindow.Presenter is OverlappedPresenter op)
            return op.State != OverlappedPresenterState.Maximized;

        return AppWindow.Presenter.Kind == AppWindowPresenterKind.CompactOverlay;
    }

    private bool CanSize(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && op.IsResizable && (op.State != OverlappedPresenterState.Maximized);
    }

    private bool CanMinimize(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && op.IsMinimizable;
    }

    private bool CanMaximize(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && op.IsMaximizable && (op.State != OverlappedPresenterState.Maximized);
    }

    public WindowState WindowState
    {
        get
        {
            if (AppWindow.Presenter is OverlappedPresenter op)
            {
                switch (op.State)
                {
                    case OverlappedPresenterState.Minimized: return WindowState.Minimized;
                    case OverlappedPresenterState.Maximized: return WindowState.Maximized;
                    case OverlappedPresenterState.Restored: return WindowState.Normal;
                }
            }

            return WindowState.Normal;
        }

        set
        {
            if (AppWindow.Presenter is OverlappedPresenter op)
            {
                switch (value)
                {
                    case WindowState.Minimized: op.Minimize(); break;
                    case WindowState.Maximized: op.Maximize(); break;
                    case WindowState.Normal: op.Restore(); break;
                }
            }
            else
            {
                Debug.Assert(value == WindowState.Normal);
            }
        }
    }

    public RectInt32 RestoreBounds
    {
        get => new RectInt32(restorePosition.X, restorePosition.Y, restoreSize.Width, restoreSize.Height);
    }

    public static int ConvertToDeviceSize(double value, double scaleFactor) => Convert.ToInt32(Math.Clamp(value * scaleFactor, 0, short.MaxValue));

    public double GetScaleFactor()
    {
        if ((Content is not null) && (Content.XamlRoot is not null))
            return Content.XamlRoot.RasterizationScale;

        double dpi = PInvoke.GetDpiForWindow((HWND)WindowPtr);
        return dpi / 96.0;
    }

    protected void ClearWindowDragRegions()
    {
        // allow mouse interaction with menu fly outs,  
        // including clicks anywhere in the client area used to dismiss the menu
        if (AppWindowTitleBar.IsCustomizationSupported())
            inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
    }

    protected void SetWindowDragRegions()
    {
        try
        {
            if ((Content is FrameworkElement layoutRoot) && layoutRoot.IsLoaded && AppWindowTitleBar.IsCustomizationSupported())
            {
                // as there is no clear distinction any more between the title bar region and the client area,
                // just treat the whole window as a title bar, click anywhere on the backdrop to drag the window.
                RectInt32 windowRect = new RectInt32(0, 0, AppWindow.ClientSize.Width, AppWindow.ClientSize.Height);
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, [windowRect]);

                List<RectInt32> rects = new List<RectInt32>();
                LocatePassThroughContent(rects, layoutRoot);
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());
            }
        }
        catch (Exception ex)
        {
            // accessing Window.Content can throw an object closed exception when
            // a menu unloaded event fires because the window is closing
            Debug.WriteLine(ex);
        }
    }

    private record class ScrollViewerBounds(in Point Offset, in Vector2 Size)
    {
        public double Top => Offset.Y;
    }

    private static void LocatePassThroughContent(List<RectInt32> rects, UIElement item, ScrollViewerBounds? bounds = null)
    {
        static Point GetOffsetFromXamlRoot(UIElement e)
        {
            GeneralTransform gt = e.TransformToVisual(null);
            return gt.TransformPoint(new Point(0, 0));
        }

        foreach (UIElement child in LogicalTreeHelper.GetChildren(item))
        {
            switch (child)
            {
                case Panel: break;

                case PuzzleView:
                case MenuBar:
                case Expander:
                case ScrollBar:
                {
                    Point offset = GetOffsetFromXamlRoot(child);
                    Vector2 actualSize = child.ActualSize;

                    if ((bounds is not null) && (offset.Y < bounds.Top)) // top clip (for vertical scroll bars) 
                    {
                        actualSize.Y -= (float)(bounds.Top - offset.Y);

                        if (actualSize.Y < 0.0)
                            return;

                        offset.Y = bounds.Top;
                    }

                    rects.Add(ScaledRect(offset, actualSize, child.XamlRoot.RasterizationScale));
                    continue;
                }

                case ScrollViewer:
                {
                    // nested scroll viewers is not supported
                    bounds = new ScrollViewerBounds(GetOffsetFromXamlRoot(child), child.ActualSize);

                    if (((ScrollViewer)child).ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        ScrollBar? vScrollBar = child.FindChild<ScrollBar>();

                        if (vScrollBar is not null)
                        {
                            Debug.Assert(vScrollBar.Name.Equals("VerticalScrollBar"));
                            rects.Add(ScaledRect(GetOffsetFromXamlRoot(vScrollBar), vScrollBar.ActualSize, child.XamlRoot.RasterizationScale));
                        }
                    }

                    break;
                }

                case CustomTitleBar: continue;

                default: break;
            }

            LocatePassThroughContent(rects, child, bounds);
        }
    }

    private static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        return new RectInt32(Convert.ToInt32(location.X * scale),
                             Convert.ToInt32(location.Y * scale),
                             Convert.ToInt32(size.X * scale),
                             Convert.ToInt32(size.Y * scale));
    }

    protected void AddDragRegionEventHandlers(UIElement item)
    {
        foreach (UIElement child in LogicalTreeHelper.GetChildren(item))
        {
            switch (child)
            {
                case Panel: break;

                case MenuBarItem mb when mb.Items.Count > 0:
                {
                    mb.Items[0].Loaded += MenuItem_Loaded;
                    mb.Items[0].Unloaded += MenuItem_Unloaded;
                    continue;
                }

                case Expander expander:
                {
                    expander.SizeChanged += UIElement_SizeChanged;
                    break;
                }

                case SimpleColorPicker picker:
                {
                    picker.FlyoutOpened += Picker_FlyoutOpened;
                    picker.FlyoutClosed += Picker_FlyoutClosed;
                    continue;
                }

                case ScrollViewer scrollViewer:
                {
                    scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
                    break;
                }

                case PuzzleView puzzleView:
                {
                    puzzleView.SizeChanged += UIElement_SizeChanged;
                    continue;
                }

                case Button:
                case TextBlock:
                case CustomTitleBar: continue; 

                default: break;
            }

            AddDragRegionEventHandlers(child);
        }

        void MenuItem_Loaded(object sender, RoutedEventArgs e) => ClearWindowDragRegions();
        void MenuItem_Unloaded(object sender, RoutedEventArgs e) => SetWindowDragRegions();
        void UIElement_SizeChanged(object sender, SizeChangedEventArgs e) => SetWindowDragRegions();
        void Picker_FlyoutOpened(SimpleColorPicker sender, bool args) => ClearWindowDragRegions();
        void Picker_FlyoutClosed(SimpleColorPicker sender, bool args) => SetWindowDragRegions();
        void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => SetWindowDragRegions();
    }
}
