namespace SudokuSolver.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal class SubClassWindow : Window
{
    public double MinWidth { get; set; }
    public double MinHeight { get; set; }
    public double InitialWidth { get; set; }
    public double InitialHeight { get; set; }
    public IntPtr WindowPtr { get; }

    private readonly SUBCLASSPROC subClassDelegate;
    protected readonly AppWindow appWindow;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;

    public SubClassWindow()
    {
        WindowPtr = WindowNative.GetWindowHandle(this);

        appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(WindowPtr));
        appWindow.Changed += AppWindow_Changed;

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)WindowPtr, subClassDelegate, 0, 0))
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

    public double GetScaleFactor()
    {
        // if the xaml hasn't loaded yet, Content.XamlRoot.RasterizationScale isn't an option
        double dpi = PInvoke.GetDpiForWindow((HWND)WindowPtr);
        return dpi / 96.0;
    }
}
