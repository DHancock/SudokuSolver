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

    private readonly double minWidth = 450;
    private readonly double minHeight = 480;

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
    private readonly DispatcherTimer dispatcherTimer;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private MenuFlyout? systemMenu;
    private WindowState windowState;
    private int scaledMinWidth;
    private int scaledMinHeight;
    private double scaleFactor;

    public WindowBase()
    {
        WindowPtr = WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)WindowPtr, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        windowState = IntialiseWindowState();
        dispatcherTimer = InitialiseDragRegionTimer();

        AppWindow.Changed += AppWindow_Changed;
        Activated += App.Instance.RecordWindowActivated;

        scaleFactor = IntialiseScaleFactor();
        scaledMinWidth = (int)(minWidth * scaleFactor);
        scaledMinHeight = (int)(minHeight * scaleFactor);

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
                if (args.DidSizeChange && (restoreSize.Height != AppWindow.Size.Height))
                {
                    SetWindowDragRegions();
                }

                restorePosition = AppWindow.Position;
                restoreSize = AppWindow.Size;
            }
        }
        else if (args.DidPresenterChange)
        {
            WindowState = IntialiseWindowState();
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
                unsafe
                {
                    MINMAXINFO* mptr = (MINMAXINFO*)lParam.Value;
                    mptr->ptMinTrackSize.X = scaledMinWidth;
                    mptr->ptMinTrackSize.Y = scaledMinHeight;
                }
                break;
            }

            case PInvoke.WM_DPICHANGED:
            {
                scaleFactor = (wParam & 0xFFFF) / 96.0;
                scaledMinWidth = (int)(minWidth * scaleFactor);
                scaledMinHeight = (int)(minHeight * scaleFactor);
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

            systemMenu.ShowAt(null, new Point(p.X / scaleFactor, p.Y / scaleFactor));
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
        return WindowState == WindowState.Maximized;
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

    private WindowState IntialiseWindowState()
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

    public WindowState WindowState
    {
        get => windowState;

        set
        {
            windowState = value;

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

    private double IntialiseScaleFactor()
    {
        double dpi = PInvoke.GetDpiForWindow((HWND)WindowPtr);
        return dpi / 96.0;
    }

    public double GetScaleFactor() => scaleFactor;

    protected void ClearWindowDragRegions()
    {
        // allow mouse interaction with menu fly outs,  
        // including clicks anywhere in the client area used to dismiss the menu
        if (AppWindowTitleBar.IsCustomizationSupported())
            inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
    }

    protected void SetWindowDragRegionsInternal()
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

    private void LocatePassThroughContent(List<RectInt32> rects, UIElement item, ScrollViewerBounds? bounds = null)
    {
        ScrollViewerBounds? parentBounds = bounds;

        static Point GetOffsetFromXamlRoot(UIElement e)
        {
            GeneralTransform gt = e.TransformToVisual(null);
            return gt.TransformPoint(new Point(0, 0));
        }

        foreach (UIElement child in LogicalTreeHelper.GetChildren(item))
        {
            if (child.XamlRoot is null)
                return;

            switch (child)
            {
                case Panel: break;

                case PuzzleView:
                case MenuBar:
                case Expander:
                case CommandBar:
                case ScrollBar:
                case TextBlock tb when tb.Inlines.Any(x => x is Hyperlink):
                {
                    Point offset = GetOffsetFromXamlRoot(child);
                    Vector2 actualSize = child.ActualSize;

                    if ((parentBounds is not null) && (offset.Y < parentBounds.Top)) // top clip (for vertical scroll bars) 
                    {
                        actualSize.Y -= (float)(parentBounds.Top - offset.Y);

                        if (actualSize.Y < 0.0)
                            return;

                        offset.Y = parentBounds.Top;
                    }

                    rects.Add(ScaledRect(offset, actualSize, scaleFactor));
                    continue;
                }

                case TabView tabView:
                {
                    // the passthrough region is the space between the header and footer
                    if ((tabView.TabStripHeader is FrameworkElement left) && (tabView.TabStripFooter is UIElement right))
                    {
                        Point leftOffset = GetOffsetFromXamlRoot(left);
                        Point rightOffset = GetOffsetFromXamlRoot(right);

                        Point topLeft = new Point(leftOffset.X + left.Margin.Left + left.ActualSize.X + left.Margin.Right, leftOffset.Y);
                        Vector2 size = new Vector2((float)(rightOffset.X - topLeft.X), right.ActualSize.Y);

                        rects.Add(ScaledRect(topLeft, size, scaleFactor));
                    }
                    break;
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
                            rects.Add(ScaledRect(GetOffsetFromXamlRoot(vScrollBar), vScrollBar.ActualSize, scaleFactor));
                        }
                    }

                    break;
                }

                default: break;
            }

            LocatePassThroughContent(rects, child, parentBounds);
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

                default: break;
            }

            AddDragRegionEventHandlers(child);
        }

        void MenuItem_Loaded(object sender, RoutedEventArgs e) => ClearWindowDragRegions();
        void MenuItem_Unloaded(object sender, RoutedEventArgs e) => SetWindowDragRegionsInternal();
        void UIElement_SizeChanged(object sender, SizeChangedEventArgs e) => SetWindowDragRegionsInternal();
        void Picker_FlyoutOpened(SimpleColorPicker sender, bool args) => ClearWindowDragRegions();
        void Picker_FlyoutClosed(SimpleColorPicker sender, bool args) => SetWindowDragRegionsInternal();
        void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => SetWindowDragRegions();
    }

    private DispatcherTimer InitialiseDragRegionTimer()
    {
        DispatcherTimer dt = new DispatcherTimer();
        dt.Interval = TimeSpan.FromMilliseconds(125);
        dt.Tick += DispatcherTimer_Tick;
        return dt;
    }

    protected void SetWindowDragRegions()
    {
        // deffer setting the drag regions while still resizing the window or scrolling
        // it's content. If the timer is already running, this resets the interval.
        dispatcherTimer.Start();
    }

    private void DispatcherTimer_Tick(object? sender, object e)
    {
        dispatcherTimer.Stop();
        SetWindowDragRegionsInternal();
    }
}
