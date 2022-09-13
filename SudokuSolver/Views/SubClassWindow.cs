namespace Sudoku.Views;

internal class SubClassWindow : Window
{
    private const double MinWidth = 388;
    private const double MinHeight = 440;
    private const double InitialWidth = 563;
    private const double InitialHeight = 614;

    protected readonly HWND hWnd;
    private readonly SUBCLASSPROC subClassDelegate;

    public SubClassWindow()
    {
        hWnd = (HWND)WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass(hWnd, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == PInvoke.WM_GETMINMAXINFO)
        {
            double scalingFactor = GetScaleFactor();

            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.X = Convert.ToInt32(MinWidth * scalingFactor);
            minMaxInfo.ptMinTrackSize.Y = Convert.ToInt32(MinHeight * scalingFactor);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    protected double GetScaleFactor()
    {
        uint dpi = PInvoke.GetDpiForWindow(hWnd);
        Debug.Assert(dpi > 0);
        return dpi / 96.0;
    }

    protected Size WindowSize
    {
        set
        {
            double scalingFactor = GetScaleFactor();

            if (!PInvoke.SetWindowPos(hWnd, (HWND)IntPtr.Zero, 0, 0, Convert.ToInt32(value.Width * scalingFactor), Convert.ToInt32(value.Height * scalingFactor), SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        get 
        {
            double scalingFactor = GetScaleFactor();

            if (!PInvoke.GetWindowRect(hWnd, out RECT lpRect))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return new Size((lpRect.right - lpRect.left) / scalingFactor, (lpRect.bottom - lpRect.top) / scalingFactor);
        }
    }

    protected void CenterInPrimaryDisplay()
    {
        if (!PInvoke.GetWindowRect(hWnd, out RECT lpRect))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        DisplayArea primary = DisplayArea.Primary;

        int top = (primary.WorkArea.Height - (lpRect.bottom - lpRect.top)) / 2;
        int left = (primary.WorkArea.Width - (lpRect.right - lpRect.left)) / 2;

        top = Math.Max(top, 0); // guarantee the title bar is visible
        left = Math.Max(left, 0);

        if (!PInvoke.SetWindowPos(hWnd, (HWND)IntPtr.Zero, left, top, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    protected WINDOWPLACEMENT GetWindowPlacement()
    {
        WINDOWPLACEMENT placement = default;

        if (!PInvoke.GetWindowPlacement(hWnd, ref placement))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return placement;
    }

    protected void SetWindowPlacement(WINDOWPLACEMENT placement)
    {
        if (placement.length == 0)  // first time, no saved state
        {
            WindowSize = new Size(InitialWidth, InitialHeight);
            CenterInPrimaryDisplay();
            Activate();
        }
        else
        {
            if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
                placement.showCmd = SHOW_WINDOW_CMD.SW_SHOWNORMAL;

            if (!PInvoke.SetWindowPlacement(hWnd, placement))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    protected void SetWindowIconFromAppIcon()
    {
        if (!PInvoke.GetModuleHandleEx(0, null, out FreeLibrarySafeHandle module))
            throw new Win32Exception(Marshal.GetLastWin32Error());

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
            throw new Win32Exception(Marshal.GetLastWin32Error());

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
