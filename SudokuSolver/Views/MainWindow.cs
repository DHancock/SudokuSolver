using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal enum WindowState { Normal, Minimized, Maximized }

internal partial class MainWindow : Window
{
    private const nuint cSubClassId = 0;

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
    public HWND WindowHandle { get; }

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
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private MenuFlyout? systemMenu;
    private int pixelMinWidth;
    private int pixelMinHeight;
    private double scaleFactor;

    public ContentDialogHelper ContentDialogHelper { get; }

    public MainWindow()
    {
        InitializeComponent();

        WindowHandle = (HWND)WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass(WindowHandle, subClassDelegate, cSubClassId, 0))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        ContentDialogHelper = new ContentDialogHelper(this);

        dispatcherTimer = InitialiseDragRegionTimer();

        AppWindow.Changed += AppWindow_Changed;
        Activated += App.Instance.RecordWindowActivated;

        scaleFactor = InitialiseScaleFactor();
        pixelMinWidth = ConvertToPixels(cMinWidth);
        pixelMinHeight = ConvertToPixels(cMinHeight);

        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        Closed -= MainWindow_Closed;

        bool success = PInvoke.RemoveWindowSubclass(WindowHandle, subClassDelegate, cSubClassId);
        Debug.Assert(success);

        AppWindow.Changed -= AppWindow_Changed;
        Activated -= App.Instance.RecordWindowActivated;

        dispatcherTimer.Stop();
        dispatcherTimer.Tick -= DispatcherTimer_Tick;

        systemMenu = null;
        Content = null;
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            if (WindowState != WindowState.Minimized)
            {
                SetWindowDragRegions();
            }

            if (WindowState == WindowState.Normal)
            {
                restoreSize = AppWindow.Size;
            }
        }

        if (args.DidPositionChange && (WindowState == WindowState.Normal))
        {
            restorePosition = AppWindow.Position;
        }
    }

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        const int HTCAPTION = 0x0002;

        switch (uMsg)
        {
            case PInvoke.WM_GETMINMAXINFO:
            {
                unsafe
                {
                    MINMAXINFO* mPtr = (MINMAXINFO*)lParam.Value;
                    mPtr->ptMinTrackSize.X = pixelMinWidth;
                    mPtr->ptMinTrackSize.Y = pixelMinHeight;
                }
                break;
            }

            case PInvoke.WM_DPICHANGED:
            {
                scaleFactor = (wParam & 0xFFFF) / 96.0;
                pixelMinWidth = ConvertToPixels(cMinWidth);
                pixelMinHeight = ConvertToPixels(cMinHeight);
                break;
            }

            case PInvoke.WM_SYSCOMMAND when (lParam == (int)VirtualKey.Space) && (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen):
            {
                HideSystemMenu();
                ShowSystemMenu(viaKeyboard: true);
                return (LRESULT)0;
            }

            case PInvoke.WM_SYSCOMMAND when (wParam == (int)SC.CLOSE) && ContentDialogHelper.IsContentDialogOpen:
            {
                return (LRESULT)0;     // disable Alt+F4
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

            case PInvoke.WM_ENDSESSION:
            {
                App.Instance.SaveStateOnEndSession();
                return (LRESULT)0;
            }
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void PostSysCommandMessage(SC command)
    {
        bool success = PInvoke.PostMessage(WindowHandle, PInvoke.WM_SYSCOMMAND, (WPARAM)(nuint)command, 0);
        Debug.Assert(success);
    }

    private void ShowSystemMenu(bool viaKeyboard)
    {
        System.Drawing.Point p = default;

        if (viaKeyboard || !PInvoke.GetCursorPos(out p) || !PInvoke.ScreenToClient(WindowHandle, ref p))
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
        closeTabCommand = new RelayCommand(ExecuteCloseTabAsync, CanClose);
        closeWindowCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE), CanClose);

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

    private bool CanClose(object? param)
    {
        return !ContentDialogHelper.IsContentDialogOpen;
    }

    private async void ExecuteCloseTabAsync(object? param)
    {
        if (CanClose(param))
        {
            await AttemptToCloseTabsAsync([Tabs.SelectedItem]);
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
                    case WindowState.Minimized when op.State != OverlappedPresenterState.Minimized:
                    {
                        op.Minimize();
                        break;
                    }
                    case WindowState.Maximized when op.State != OverlappedPresenterState.Maximized:
                    {
                        op.Maximize();
                        break;
                    }
                    case WindowState.Normal when op.State != OverlappedPresenterState.Restored:
                    {
                        op.Restore();
                        break;
                    }
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

    public int ConvertToPixels(double value)
    {
        Debug.Assert(value >= 0.0);
        Debug.Assert(scaleFactor > 0.0);

        return (int)Math.FusedMultiplyAdd(value, scaleFactor, 0.5);
    }

    private double InitialiseScaleFactor()
    {
        double dpi = PInvoke.GetDpiForWindow(WindowHandle);
        return dpi / 96.0;
    }

    private void SetWindowDragRegionsInternal()
    {
        const int cTabViewPassthroughCount = 3;

        try
        {
            RectInt32 windowRect = new RectInt32(0, 0, AppWindow.ClientSize.Width, AppWindow.ClientSize.Height);

            if (ContentDialogHelper.IsContentDialogOpen)
            {
                // this also effectively disables the caption buttons
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, [windowRect]);
            }
            else
            {
                // as there is no clear distinction any more between the title bar region and the client area,
                // just treat the whole window as a title bar, click anywhere on the backdrop to drag the window.
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, [windowRect]);

                int size = ((ITabItem)Tabs.SelectedItem).PassthroughCount + cTabViewPassthroughCount;
                RectInt32[] rects = new RectInt32[size];

                ((ITabItem)Tabs.SelectedItem).AddPassthroughContent(rects);
                AddTabViewPassthroughContent(rects);

                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, rects);
            }
        }
        catch (Exception ex)
        {
            // can throw if the window is closing
            Debug.WriteLine(ex.ToString());
        }
    }

    private void AddTabViewPassthroughContent(in RectInt32[] rects)
    {
        FrameworkElement left = (FrameworkElement)Tabs.TabStripHeader;
        FrameworkElement right = (FrameworkElement)Tabs.TabStripFooter;

        // the passthrough region for the tab header strip is the space between the header and footer
        Point leftOffset = Utils.GetOffsetFromXamlRoot(left);
        Point rightOffset = Utils.GetOffsetFromXamlRoot(right);

        Point topLeft = new Point(leftOffset.X + left.Margin.Left + left.ActualSize.X + left.Margin.Right, rightOffset.Y + Tabs.Padding.Top);
        Vector2 size = new Vector2((float)(rightOffset.X - topLeft.X), right.ActualSize.Y);

        rects[rects.Length - 1] = Utils.ScaledRect(topLeft, size, scaleFactor);
        // the header is also the window icon area
        rects[rects.Length - 2] = Utils.ScaledRect(leftOffset, left.ActualSize, scaleFactor);
        rects[rects.Length - 3] = Utils.GetPassthroughRect(JumpToTabButton);
    }

    private DispatcherTimer InitialiseDragRegionTimer()
    {
        DispatcherTimer dt = new DispatcherTimer();
        dt.Interval = TimeSpan.FromMilliseconds(50);
        dt.Tick += DispatcherTimer_Tick;
        return dt;
    }

    public void SetWindowDragRegions()
    {
        // defer setting the drag regions while still resizing the window or scrolling
        // it's content. If the timer is already running, this resets the interval.
        dispatcherTimer.Start();
    }  

    private void DispatcherTimer_Tick(object? sender, object e)
    {
        dispatcherTimer.Stop();
        SetWindowDragRegionsInternal();
    }

    public void ContentDialogOpened()
    {
        // workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/5739
        // focus can escape a content dialog when access keys are shown via the alt key...
        // (it makes no difference if the content dialog itself has any access keys)
        ((ITabItem)Tabs.SelectedItem).EnableMenuAccessKeys(enable: false);

        OverlappedPresenter op = (OverlappedPresenter)AppWindow.Presenter;
        op.IsResizable = false;
        op.IsMinimizable = false;

        UpdateCaptionButtonColours();
        SetWindowDragRegionsInternal();
    }

    public void ContentDialogClosing()
    {
        ((ITabItem)Tabs.SelectedItem).EnableMenuAccessKeys(enable: true);

        OverlappedPresenter op = (OverlappedPresenter)AppWindow.Presenter;
        op.IsResizable = true;
        op.IsMinimizable = true;
    }

    public void ContentDialogClosed()
    {
        UpdateCaptionButtonColours();
        SetWindowDragRegionsInternal();
    }
}
