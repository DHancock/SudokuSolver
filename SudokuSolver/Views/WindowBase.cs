using RelayCommand = SudokuSolver.ViewModels.RelayCommand;

namespace SudokuSolver.Views;

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

        // sub class to set a minimum window size
        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)WindowPtr, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        AppWindow.Changed += AppWindow_Changed;
        Activated += App.Instance.RecordWindowActivated;

        RestoreCommand = new RelayCommand(o => PostSysCommandMessage(SC.RESTORE), o => WindowState == WindowState.Maximized);
        MoveCommand = new RelayCommand(o => PostSysCommandMessage(SC.MOVE), o => WindowState != WindowState.Maximized);
        SizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.SIZE), o => WindowState != WindowState.Maximized);
        MinimizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MINIMIZE));
        MaximizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MAXIMIZE), o => WindowState != WindowState.Maximized);
        CloseCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE));
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (WindowState == WindowState.Normal)
        {
            if (args.DidPositionChange)
                restorePosition = AppWindow.Position;

            if (args.DidSizeChange)
                restoreSize = AppWindow.Size;
        }

        if (args.DidSizeChange)
        {
            RestoreCommand.RaiseCanExecuteChanged();
            MoveCommand.RaiseCanExecuteChanged();
            SizeCommand.RaiseCanExecuteChanged();
            MaximizeCommand.RaiseCanExecuteChanged();
        }
    }

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        const int VK_SPACE = 0x0020;
        const int HTCAPTION = 0x0002;

        if (uMsg == PInvoke.WM_GETMINMAXINFO)
        {
            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            double scaleFactor = GetScaleFactor();
            minMaxInfo.ptMinTrackSize.X = Math.Max(ConvertToDeviceSize(MinWidth, scaleFactor), minMaxInfo.ptMinTrackSize.X);
            minMaxInfo.ptMinTrackSize.Y = Math.Max(ConvertToDeviceSize(MinHeight, scaleFactor), minMaxInfo.ptMinTrackSize.Y);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }
        else if (uMsg == PInvoke.WM_SYSCOMMAND)
        {
            if (lParam == VK_SPACE)
            {
                ShowSytemMenu();
                return (LRESULT)0;
            }
        }
        else if ((uMsg == PInvoke.WM_NCRBUTTONUP) && (wParam == HTCAPTION))
        {
            ShowSytemMenu();
            return (LRESULT)0;
        }
        else if ((uMsg == PInvoke.WM_NCLBUTTONDOWN) && (wParam == HTCAPTION))
        {
            HideSystemMenu();
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void PostSysCommandMessage(SC command)
    {
        bool success = PInvoke.PostMessage((HWND)WindowPtr, PInvoke.WM_SYSCOMMAND, (WPARAM)(nuint)command, 0);
        Debug.Assert(success);
    }

    private void ShowSytemMenu()
    {
        if ((systemMenu is null) && (Content is FrameworkElement root))
            systemMenu = root.Resources["SystemMenuFlyout"] as MenuFlyout;

        if ((systemMenu is not null) &&
            (Content.XamlRoot is not null) &&
            PInvoke.GetCursorPos(out System.Drawing.Point p) &&
            PInvoke.ScreenToClient((HWND)WindowPtr, ref p))
        {
            double scale = Content.XamlRoot.RasterizationScale;
            systemMenu.ShowAt(null, new Windows.Foundation.Point(p.X / scale, p.Y / scale));
        }
    }

    private void HideSystemMenu() => systemMenu?.Hide();

    public void PostCloseMessage() => PostSysCommandMessage(SC.CLOSE);

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
        }
    }

    public RectInt32 RestoreBounds
    {
        get => new RectInt32(restorePosition.X, restorePosition.Y, restoreSize.Width, restoreSize.Height);
    }

    public static int ConvertToDeviceSize(double value, double scalefactor) => Convert.ToInt32(Math.Clamp(value * scalefactor, 0, short.MaxValue));

    public double GetScaleFactor()
    {
        // if the xaml hasn't loaded yet, Content.XamlRoot.RasterizationScale isn't an option
        double dpi = PInvoke.GetDpiForWindow((HWND)WindowPtr);
        return dpi / 96.0;
    }
}
