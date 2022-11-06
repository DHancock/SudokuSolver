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

    public SubClassWindow()
    {
        hWnd = (HWND)WindowNative.GetWindowHandle(this);
        appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));
        
        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass(hWnd, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
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

    public Rect RestoreBounds
    {
        get
        {
            if (WindowState == WindowState.Normal)
                return new Rect(appWindow.Position.X, appWindow.Position.Y, appWindow.Size.Width, appWindow.Size.Height);

            int deltaX = 0, deltaY = 0;

            // unless it's a tool window, the normal position is relative to the working area
            if (!IsToolWindow)
            {
                DisplayArea? display = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.None);

                if (display is not null)
                {
                    deltaX = display.WorkArea.X - display.OuterBounds.X;
                    deltaY = display.WorkArea.Y - display.OuterBounds.Y;
                }
            }

            WINDOWPLACEMENT wp = default;

            if (!PInvoke.GetWindowPlacement(hWnd, ref wp))
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            return new Rect(wp.rcNormalPosition.left + deltaX, wp.rcNormalPosition.top + deltaY, 
                            wp.rcNormalPosition.Width, wp.rcNormalPosition.Height);
        }
    }

    private bool IsToolWindow => ((uint)StyleEx & (uint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) > 0;

    public int StyleEx
    {
        get
        {
            int styleEx = PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            if (styleEx == 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            return styleEx;
        }
    }

    public static Rect GetWorkingAreaOfClosestMonitor(Rect windowBounds)
    {
        DisplayArea area = DisplayArea.GetFromRect(ConvertToRectInt32(windowBounds), DisplayAreaFallback.Nearest);
        return new Rect(area.WorkArea.X, area.WorkArea.Y, area.WorkArea.Width, area.WorkArea.Height);
    }

    private static RECT ConvertToRECT(Rect input)
    {
        RECT output = new RECT();

        // avoids accumulating rounding errors
        output.top = Convert.ToInt32(input.Y);
        output.left = Convert.ToInt32(input.X);
        output.bottom = output.top + Convert.ToInt32(input.Height);
        output.right = output.left + Convert.ToInt32(input.Width);

        return output;
    }

    protected static RectInt32 ConvertToRectInt32(Rect input)
    {
        RECT intermediate = ConvertToRECT(input);
        return new RectInt32(intermediate.X, intermediate.Y, intermediate.Width, intermediate.Height);
    }

    protected double GetScaleFactor()
    {
        uint dpi = PInvoke.GetDpiForWindow(hWnd);
        Debug.Assert(dpi > 0);
        return dpi / 96.0;
    }

    protected RectInt32 CenterInPrimaryDisplay()
    {
        double scalingFactor = GetScaleFactor();
        RectInt32 pos = new RectInt32();

        pos.Width = Convert.ToInt32(cInitialWidth * scalingFactor);
        pos.Height = Convert.ToInt32(cInitialHeight * scalingFactor);

        DisplayArea primary = DisplayArea.Primary;

        pos.Y = (primary.WorkArea.Height - pos.Height) / 2;
        pos.X = (primary.WorkArea.Width - pos.Width) / 2;

        // guarantee title bar is visible
        pos.Y = Math.Max(pos.Y, primary.WorkArea.Y);
        pos.X = Math.Max(pos.X, primary.WorkArea.X);

        return pos;
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
