namespace Sudoku.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal class SubClassWindow : Window
{
    private int minDeviceWidth;
    private int minDeviceHeight;
    private double initialWidth;
    private double initialHeight;

    public event DpiChangedEventHandler? DpiChanged;
    private double currentDpi;

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

        currentDpi = PInvoke.GetDpiForWindow(hWnd);
        DpiChanged += SubClassWindow_DpiChanged;

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
            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.X = Math.Max(minDeviceWidth, minMaxInfo.ptMinTrackSize.X);
            minMaxInfo.ptMinTrackSize.Y = Math.Max(minDeviceHeight, minMaxInfo.ptMinTrackSize.Y);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }
        else if (uMsg == PInvoke.WM_DPICHANGED)
        {
            double newDpi = wParam & 0x0000FFFF;
            double oldDpi = currentDpi;
            currentDpi = newDpi;

            DpiChanged?.Invoke(this, new DpiChangedEventArgs(oldDpi, newDpi));
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

    public double MinWidth
    {
        set => minDeviceWidth = ConvertToDeviceSize(value);
    }

    public double MinHeight
    {
        set => minDeviceHeight = ConvertToDeviceSize(value);
    }

    public double InitialWidth
    {
        set => initialWidth = value;
    }

    public double InitialHeight
    {
        set => initialHeight = value;
    }

    private int ConvertToDeviceSize(double value) => Convert.ToInt32(Math.Clamp(value * (currentDpi / 96.0), 0, short.MaxValue));

    private void SubClassWindow_DpiChanged(object sender, DpiChangedEventArgs args)
    {
        minDeviceWidth = Convert.ToInt32((minDeviceWidth / args.OldDpi) * args.NewDpi);
        minDeviceHeight = Convert.ToInt32((minDeviceHeight / args.OldDpi) * args.NewDpi);
    }


    protected RectInt32 CenterInPrimaryDisplay()
    {
        RectInt32 workArea = DisplayArea.Primary.WorkArea;
        RectInt32 windowArea;

        windowArea.Width = ConvertToDeviceSize(initialWidth);
        windowArea.Height = ConvertToDeviceSize(initialHeight);

        windowArea.Width = Math.Min(windowArea.Width, workArea.Width);
        windowArea.Height = Math.Min(windowArea.Height, workArea.Height);

        windowArea.Y = (workArea.Height - windowArea.Height) / 2;
        windowArea.X = (workArea.Width - windowArea.Width) / 2;

        // guarantee title bar is visible, the minimum window size may trump working area
        windowArea.Y = Math.Max(windowArea.Y, workArea.Y);
        windowArea.X = Math.Max(windowArea.X, workArea.X);

        return windowArea;
    }

    protected void SetWindowIconFromAppIcon()
    {
        if (!PInvoke.GetModuleHandleEx(0, null, out FreeLibrarySafeHandle module))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        int size = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXICON);

        if (size == 0)
            throw new Win32Exception(); // get last error doesn't provide any extra information 

        const string cAppIconResourceId = "#32512";
        SafeFileHandle hIcon = PInvoke.LoadImage(module, cAppIconResourceId, GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_DEFAULTCOLOR);

        if (hIcon.IsInvalid)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        try
        {
            appWindow.SetIcon(Win32Interop.GetIconIdFromIcon(hIcon.DangerousGetHandle()));
        }
        finally
        {
            hIcon.SetHandleAsInvalid(); // SafeFileHandle must not release the shared icon
        }
    }
}

public delegate void DpiChangedEventHandler(object sender, DpiChangedEventArgs e);

public class DpiChangedEventArgs
{
    public double OldDpi { get; init; }
    public double NewDpi { get; init; }

    public DpiChangedEventArgs(double oldDpi, double newDpi)
    {
        OldDpi = oldDpi;
        NewDpi = newDpi;
    }
}

