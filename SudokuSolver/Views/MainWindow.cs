using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal partial class MainWindow : Window
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

    private const double cMinWidth = 410;
    private const double cMinHeight = 480;
    public const double cInitialWidth = 563;
    public const double cInitialHeight = 614;
    public IntPtr WindowPtr { get; }

    private RelayCommand? restoreCommand;
    private RelayCommand? moveCommand;
    private RelayCommand? sizeCommand;
    private RelayCommand? minimizeCommand;
    private RelayCommand? maximizeCommand;
    private RelayCommand? closeTabCommand;
    private RelayCommand? closeWindowCommand;

    private readonly InputNonClientPointerSource inputNonClientPointerSource;
    private readonly SUBCLASSPROC subClassDelegate;
    private readonly DispatcherTimer dispatcherTimer;
    private bool cancelDragRegionTimerEvent = false;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private MenuFlyout? systemMenu;
    private int scaledMinWidth;
    private int scaledMinHeight;
    private double scaleFactor;

    public MainWindow()
    {
        InitializeComponent();

        WindowPtr = WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)WindowPtr, subClassDelegate, 0, 0))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        dispatcherTimer = InitialiseDragRegionTimer();

        AppWindow.Changed += AppWindow_Changed;
        Activated += App.Instance.RecordWindowActivated;

        scaleFactor = InitialiseScaleFactor();
        scaledMinWidth = ConvertToDeviceSize(cMinWidth);
        scaledMinHeight = ConvertToDeviceSize(cMinHeight);

        Closed += (s, e) =>
        {
            cancelDragRegionTimerEvent = true;
            dispatcherTimer.Stop();
        };
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
                scaledMinWidth = ConvertToDeviceSize(cMinWidth);
                scaledMinHeight = ConvertToDeviceSize(cMinHeight);
                break;
            }

            case PInvoke.WM_SYSCOMMAND when (lParam == VK_SPACE) && (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen):
            {
                HideSystemMenu();
                ShowSystemMenu(viaKeyboard: true);
                return (LRESULT)0;
            }

            case PInvoke.WM_NCRBUTTONUP when wParam == HTCAPTION:
            {
                HideSystemMenu();
                ShowSystemMenu(viaKeyboard: false);
                return (LRESULT)0;
            }

            case PInvoke.WM_NCLBUTTONDOWN when wParam == HTCAPTION:
            {
                HideSystemMenu();
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

    private void ShowSystemMenu(bool viaKeyboard)
    {
        System.Drawing.Point p = default;

        if (viaKeyboard || !PInvoke.GetCursorPos(out p) || !PInvoke.ScreenToClient((HWND)WindowPtr, ref p))
        {
            p.X = 3;
            p.Y = AppWindow.TitleBar.Height;
        }

        systemMenu ??= BuildSystemMenu();
        systemMenu.ShowAt(null, new Point(p.X / scaleFactor, p.Y / scaleFactor));
    }

    private void HideSystemMenu()
    {
        if ((systemMenu is not null) && systemMenu.IsOpen)
        {
            systemMenu.Hide();
        }
    }

    private MenuFlyout BuildSystemMenu()
    {
        const string cStyleKey = "DefaultMenuFlyoutPresenterStyle";
        const string cPaddingKey = "MenuFlyoutItemThemePaddingNarrow";

        Debug.Assert(Content is FrameworkElement);
        Debug.Assert(((FrameworkElement)Content).Resources.ContainsKey(cStyleKey));
        Debug.Assert(((FrameworkElement)Content).Resources.ContainsKey(cPaddingKey));

        restoreCommand = new RelayCommand(o => PostSysCommandMessage(SC.RESTORE), CanRestore);
        moveCommand = new RelayCommand(o => PostSysCommandMessage(SC.MOVE), CanMove);
        sizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.SIZE), CanSize);
        minimizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MINIMIZE), CanMinimize);
        maximizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MAXIMIZE), CanMaximize);
        closeTabCommand = new RelayCommand(ExecuteCloseTabAsync, CanCloseTab);
        closeWindowCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE));

        MenuFlyout menuFlyout = new MenuFlyout()
        {
            XamlRoot = Content.XamlRoot,
            MenuFlyoutPresenterStyle = (Style)((FrameworkElement)Content).Resources[cStyleKey],
            OverlayInputPassThroughElement = Content,
        };

        // always use narrow padding (the first time the menu is opened it may use normal padding, other times narrow)
        Thickness narrow = (Thickness)((FrameworkElement)Content).Resources[cPaddingKey];
        ResourceLoader rl = App.Instance.ResourceLoader;

        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = rl.GetString("SystemMenuRestore"), Command = restoreCommand, Padding = narrow, AccessKey = "R"});
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = rl.GetString("SystemMenuMove"), Command = moveCommand, Padding = narrow, AccessKey = "M" });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = rl.GetString("SystemMenuSize"), Command = sizeCommand, Padding = narrow, AccessKey = "S" });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = rl.GetString("SystemMenuMinimize"), Command = minimizeCommand, Padding = narrow, AccessKey = "N" });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = rl.GetString("SystemMenuMaximize"), Command = maximizeCommand, Padding = narrow, AccessKey = "X" });
        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        MenuFlyoutItem closeTabItem = new MenuFlyoutItem() { Text = rl.GetString("SystemMenuCloseTab"), Command = closeTabCommand, Padding = narrow, AccessKey = "W" };
        // the accelerator is disabled to avoid two close messages (from either the puzzle tabs file menu or the settings tab context menu )
        closeTabItem.KeyboardAccelerators.Add(new() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.W, IsEnabled = false });
        menuFlyout.Items.Add(closeTabItem);

        MenuFlyoutItem closeWindowItem = new MenuFlyoutItem() { Text = rl.GetString("SystemMenuCloseWindow"), Command = closeWindowCommand, Padding = narrow, AccessKey = "C" };
        // the accelerator is disabled to avoid two close messages (the original system menu still exists)
        closeWindowItem.KeyboardAccelerators.Add(new() { Modifiers = VirtualKeyModifiers.Menu, Key = VirtualKey.F4, IsEnabled = false });
        menuFlyout.Items.Add(closeWindowItem);

        return menuFlyout;
    }

    public void PostCloseMessage() => PostSysCommandMessage(SC.CLOSE);

    private bool CanRestore(object? param)
    {
        return WindowState == WindowState.Maximized;
    }

    private bool CanMove(object? param)
    {
        if (AppWindow.Presenter is OverlappedPresenter op)
        {
            return op.State != OverlappedPresenterState.Maximized;
        }

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

    private bool CanCloseTab(object? param)
    {
        return !IsContentDialogOpen();
    }

    private async void ExecuteCloseTabAsync(object? param)
    {
        if (CanCloseTab(param))
        {
            List<object> tabs = new List<object>();
            tabs.Add(Tabs.SelectedItem);
            await AttemptToCloseTabsAsync(tabs);
        }
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

    public int ConvertToDeviceSize(double value)
    {
        Debug.Assert(scaleFactor > 0);
        return Convert.ToInt32(value * scaleFactor);
    }

    private double InitialiseScaleFactor()
    {
        double dpi = PInvoke.GetDpiForWindow((HWND)WindowPtr);
        return dpi / 96.0;
    }

    private void ClearWindowDragRegions()
    {
        // Guard against race hazards. If a tab is selected using right click a size changed event is generated
        // and the timer started. The drag regions will be cleared when the context menu is opened, followed
        // by the timer event which could then reset the drag regions while the menu was still open. Stopping
        // the timer isn't enough because the tick event may have already been queued (on the same thread).
        cancelDragRegionTimerEvent = true;

        // allow mouse interaction with menu fly outs,  
        // including clicks anywhere in the client area used to dismiss the menu
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
        }
    }

    private void SetWindowDragRegionsInternal()
    {
        const int cInitialCapacity = 9;

        cancelDragRegionTimerEvent = false;

        try
        {
            if ((Content is FrameworkElement layoutRoot) && layoutRoot.IsLoaded && AppWindowTitleBar.IsCustomizationSupported())
            {
                // as there is no clear distinction any more between the title bar region and the client area,
                // just treat the whole window as a title bar, click anywhere on the backdrop to drag the window.
                RectInt32 windowRect = new RectInt32(0, 0, AppWindow.ClientSize.Width, AppWindow.ClientSize.Height);
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, [windowRect]);

                List<RectInt32> rects = new List<RectInt32>(cInitialCapacity);
                LocatePassThroughContent(rects, layoutRoot);
                Debug.Assert(rects.Count <= cInitialCapacity);

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

        foreach (UIElement child in LogicalTreeHelper.GetChildren(item))
        {
            if (child.XamlRoot is null)
            {
                return;
            }

            switch (child)
            {
                case Panel: break;

                case PuzzleView:
                case MenuBar:
                case Expander:
                case Button:
                case CommandBar:
                case ScrollBar:
                case TextBlock tb when ReferenceEquals(tb, tb.Tag): // it contains a hyperlink
                {
                    Point offset = GetOffsetFromXamlRoot(child);
                    Vector2 actualSize = child.ActualSize;

                    if ((parentBounds is not null) && (offset.Y < parentBounds.Top)) // top clip (for vertical scroll bars) 
                    {
                        actualSize.Y -= (float)(parentBounds.Top - offset.Y);

                        if (actualSize.Y < 0.1)
                            continue;

                        offset.Y = parentBounds.Top;
                    }

                    rects.Add(ScaledRect(offset, actualSize, scaleFactor));
                    continue;
                }

                case TabView tabView:
                {
                    // the passthrough region for the tabs is the space between the header and footer
                    if ((tabView.TabStripHeader is FrameworkElement left) && (tabView.TabStripFooter is UIElement right))
                    {
                        Point leftOffset = GetOffsetFromXamlRoot(left);
                        Point rightOffset = GetOffsetFromXamlRoot(right);

                        Point topLeft = new Point(leftOffset.X + left.Margin.Left + left.ActualSize.X + left.Margin.Right, rightOffset.Y + tabView.Padding.Top);
                        Vector2 size = new Vector2((float)(rightOffset.X - topLeft.X), right.ActualSize.Y);

                        rects.Add(ScaledRect(topLeft, size, scaleFactor));

                        // the header is also the window icon area
                        rects.Add(ScaledRect(leftOffset, left.ActualSize, scaleFactor));

                        // add the drop down button at the left edge of the footer
                        LocatePassThroughContent(rects, right);
                    }

                    if (tabView.SelectedItem is TabViewItem tabViewItem)
                    {
                        LocatePassThroughContent(rects, tabViewItem);
                    }

                    continue;
                }

                case ScrollViewer:
                {
                    // nested scroll viewers is not supported
                    bounds = new ScrollViewerBounds(GetOffsetFromXamlRoot(child), child.ActualSize);

                    if (((ScrollViewer)child).ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        ScrollBar? vScrollBar = child.FindChild<ScrollBar>("VerticalScrollBar");
                        Debug.Assert(vScrollBar is not null);

                        if (vScrollBar is not null)
                        {
                            rects.Add(ScaledRect(GetOffsetFromXamlRoot(vScrollBar), vScrollBar.ActualSize, scaleFactor));
                        }
                    }

                    break;
                }


                default: break;
            }

            LocatePassThroughContent(rects, child, bounds);
        }

        static Point GetOffsetFromXamlRoot(UIElement e)
        {
            GeneralTransform gt = e.TransformToVisual(null);
            return gt.TransformPoint(new Point(0, 0));
        }
    }

    private static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        return new RectInt32(Convert.ToInt32(location.X * scale),
                             Convert.ToInt32(location.Y * scale),
                             Convert.ToInt32(size.X * scale),
                             Convert.ToInt32(size.Y * scale));
    }

    private void AddDragRegionEventHandlers(UIElement item)
    {
        if (item.ContextFlyout is MenuFlyout menuFlyout)  // menu flyouts are not UIElements
        {
            menuFlyout.Opened += MenuFlyout_Opened;
            menuFlyout.Closed += MenuFlyout_Closed;
        }

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

                case PuzzleView puzzleView:
                {
                    puzzleView.SizeChanged += UIElement_SizeChanged;
                    continue;
                }

                case ScrollViewer scrollViewer:
                {
                    scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
                    break;
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
        void MenuFlyout_Opened(object? sender, object e) => ClearWindowDragRegions();
        void MenuFlyout_Closed(object? sender, object e) => SetWindowDragRegionsInternal();
        void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => SetWindowDragRegions();
    }

    private DispatcherTimer InitialiseDragRegionTimer()
    {
        DispatcherTimer dt = new DispatcherTimer();
        dt.Interval = TimeSpan.FromMilliseconds(50);
        dt.Tick += DispatcherTimer_Tick;
        return dt;
    }

    private void SetWindowDragRegions()
    {
        // defer setting the drag regions while still resizing the window or scrolling
        // it's content. If the timer is already running, this resets the interval.
        dispatcherTimer.Start();
    }

    private void DispatcherTimer_Tick(object? sender, object e)
    {
        dispatcherTimer.Stop();

        if (!cancelDragRegionTimerEvent)
        {
            SetWindowDragRegionsInternal();
        }
    }
}
