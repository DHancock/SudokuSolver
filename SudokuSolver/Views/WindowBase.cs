using RelayCommand = SudokuSolver.ViewModels.RelayCommand;

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
            }
        }
        else if (args.DidPresenterChange) // including properties of the current presenter
        {
            if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                HideSystemMenu();
            else
                UpdateSytemMenuItemsEnabledState();
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
                    ShowSytemMenu(viaKeyboard: true);
                    return (LRESULT)0;
                }

                break;
            }

            case PInvoke.WM_NCRBUTTONUP:
            {
                if (wParam == HTCAPTION)
                {
                    HideSystemMenu();
                    ShowSytemMenu(viaKeyboard: false);
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

    private void AddCloseKeyboardAcceleratorTextOverride()
    {
        // If the Close menu item actually had a keyboard accelerator then two close messages would be  
        // generated when the user typed Alt+F4, so just fake the existence of it for the user...
        if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.MenuFlyoutItem", "KeyboardAcceleratorTextOverride"))
        {
            Debug.Assert(systemMenu is not null);
            MenuFlyoutItem mfi = (MenuFlyoutItem)systemMenu.Items[systemMenu.Items.Count - 1];
            mfi.KeyboardAcceleratorTextOverride = "Alt+F4";
        }
    }

    private void ShowSytemMenu(bool viaKeyboard)
    {
        if ((systemMenu is null) && (Content is FrameworkElement root))
        {
            systemMenu = root.Resources["SystemMenuFlyout"] as MenuFlyout;

            if (systemMenu is not null)
                AddCloseKeyboardAcceleratorTextOverride();
        }

        if ((systemMenu is not null) && (Content.XamlRoot is not null))
        {
            System.Drawing.Point p = default;

            if (!viaKeyboard)
            {
                if (!PInvoke.GetCursorPos(out p) || !PInvoke.ScreenToClient((HWND)WindowPtr, ref p))
                {
                    Debug.Fail("Failed to obtain cursor position.");
                    return;
                }
            }
            else
            {
                p.X = 3;
                p.Y = AppWindow.TitleBar.Height;
            }

            double scale = Content.XamlRoot.RasterizationScale;
            systemMenu.ShowAt(null, new Windows.Foundation.Point(p.X / scale, p.Y / scale));
        }
    }

    private void HideSystemMenu()
    {
        if ((systemMenu is not null) && systemMenu.IsOpen)
            systemMenu.Hide();
    }

    private void UpdateSytemMenuItemsEnabledState()
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

    public static int ConvertToDeviceSize(double value, double scalefactor) => Convert.ToInt32(Math.Clamp(value * scalefactor, 0, short.MaxValue));

    public double GetScaleFactor()
    {
        if ((Content is not null) && (Content.XamlRoot is not null))
            return Content.XamlRoot.RasterizationScale;

        double dpi = PInvoke.GetDpiForWindow((HWND)WindowPtr);
        return dpi / 96.0;
    }
}
