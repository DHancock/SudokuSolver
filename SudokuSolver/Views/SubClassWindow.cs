namespace Sudoku.Views;

public enum WindowState { Normal, Minimized, Maximized } 

internal class SubClassWindow : Window
{
    private const double cMinWidth = 388;
    private const double cMinHeight = 440;
    private const double cInitialWidth = 563;
    private const double cInitialHeight = 614;

    protected readonly HWND hWnd;
    private readonly SUBCLASSPROC subClassDelegate;
    protected readonly AppWindow appWindow;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;

    public SubClassWindow()
    {
        hWnd = (HWND)WindowNative.GetWindowHandle(this);

        appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));
        appWindow.Changed += AppWindow_Changed;

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass(hWnd, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (WindowState == WindowState.Normal)
        {
            if (args.DidPositionChange)
                restorePosition = appWindow.Position;

            if (args.DidSizeChange)
                restoreSize = appWindow.Size;
        }
    }

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == PInvoke.WM_GETMINMAXINFO)
        {
            double scalingFactor = GetScaleFactor();

            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.X = Convert.ToInt32(cMinWidth * scalingFactor);
            minMaxInfo.ptMinTrackSize.Y = Convert.ToInt32(cMinHeight * scalingFactor);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public WindowState WindowState
    {
        get
        {
            if (appWindow.Presenter is OverlappedPresenter op)
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
            if (appWindow.Presenter is OverlappedPresenter op)
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

    protected double GetScaleFactor()
    {
        uint dpi = PInvoke.GetDpiForWindow(hWnd);
        Debug.Assert(dpi > 0);
        return dpi / 96.0;
    }

    protected RectInt32 CenterInPrimaryDisplay()
    {
        RectInt32 workArea = DisplayArea.Primary.WorkArea;

        double scalingFactor = GetScaleFactor();
        RectInt32 position;

        position.Width = Convert.ToInt32(cInitialWidth * scalingFactor);
        position.Height = Convert.ToInt32(cInitialHeight * scalingFactor);

        position.Width = Math.Min(position.Width, workArea.Width);
        position.Height = Math.Max(position.Height, workArea.Height);

        position.Y = (workArea.Height - position.Height) / 2;
        position.X = (workArea.Width - position.Width) / 2;

        // guarantee title bar is visible, the minimum window size may trump working area
        position.Y = Math.Max(position.Y, workArea.Y);
        position.X = Math.Max(position.X, workArea.X);

        return position;
    }

    protected void SetWindowIconFromAppIcon()
    {
        if (!PInvoke.GetModuleHandleEx(0, null, out FreeLibrarySafeHandle module))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        WPARAM ICON_SMALL = 0;
        WPARAM ICON_BIG = 1;
        const string cAppIconResourceId = "#32512";

        SetWindowIcon(module, cAppIconResourceId, ICON_SMALL, PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON));
        SetWindowIcon(module, cAppIconResourceId, ICON_BIG, PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXICON));
    }

    private void SetWindowIcon(FreeLibrarySafeHandle module, string iconId, WPARAM iconType, int size)
    {
        const uint WM_SETICON = 0x0080;

        SafeFileHandle hIcon = PInvoke.LoadImage(module, iconId, GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_DEFAULTCOLOR);

        if (hIcon.IsInvalid)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        try
        {
            LRESULT previousIcon = PInvoke.SendMessage(hWnd, WM_SETICON, iconType, hIcon.DangerousGetHandle());
            Debug.Assert(previousIcon == (LRESULT)0);
        }
        finally
        {
            hIcon.SetHandleAsInvalid(); // SafeFileHandle must not release the shared icon
        }
    }
}
