namespace Sudoku.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal class SubClassWindow : Window
{
    public double MinWidth { get; set; }
    public double MinHeight { get; set; }
    public double InitialWidth { get; set; }
    public double InitialHeight { get; set; }

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
            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            double scaleFactor = GetScaleFactor();
            minMaxInfo.ptMinTrackSize.X = Math.Max(ConvertToDeviceSize(MinWidth, scaleFactor), minMaxInfo.ptMinTrackSize.X);
            minMaxInfo.ptMinTrackSize.Y = Math.Max(ConvertToDeviceSize(MinHeight, scaleFactor), minMaxInfo.ptMinTrackSize.Y);
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

    public static int ConvertToDeviceSize(double value, double scalefactor) => Convert.ToInt32(Math.Clamp(value * scalefactor, 0, short.MaxValue));

    protected double GetScaleFactor()
    {
        // The xaml may not have loaded yet, so Content.XamlRoot.RasterizationScale isn't an option here
        double dpi = PInvoke.GetDpiForWindow(hWnd);
        return dpi / 96.0;
    }

    protected void SetWindowIconFromAppIcon()
    {
        if (!PInvoke.GetModuleHandleEx(0, null, out FreeLibrarySafeHandle module))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        int size = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXICON);

        if (size == 0)
            throw new Win32Exception(); // get last error doesn't provide any extra information 

        SafeFileHandle hIcon = PInvoke.LoadImage(module, $"#{App.cIconResourceID}", GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_DEFAULTCOLOR);

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
