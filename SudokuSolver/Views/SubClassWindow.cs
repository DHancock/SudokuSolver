namespace Sudoku.Views;


internal class SubClassWindow : Window
{
    public const double MinWidth = 388;
    public const double MinHeight = 440;
    public const double InitialWidth = 563;
    public const double InitialHeight = 614;

    protected readonly HWND hWnd;
    private readonly SUBCLASSPROC subClassDelegate;

    public SubClassWindow()
    {
        hWnd = (HWND)WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass(hWnd, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        const uint WM_GETMINMAXINFO = 0x0024;

        if (uMsg == WM_GETMINMAXINFO)
        {
            uint dpi = PInvoke.GetDpiForWindow(hWnd);
            double scalingFactor = dpi / 96.0;

            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.x = (int)(MinWidth * scalingFactor);
            minMaxInfo.ptMinTrackSize.y = (int)(MinHeight * scalingFactor);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    protected Size WindowSize
    {
        set
        {
            uint dpi = PInvoke.GetDpiForWindow(hWnd);
            double scalingFactor = dpi / 96.0;

            if (!PInvoke.SetWindowPos(hWnd, (HWND)IntPtr.Zero, 0, 0, (int)(value.Width * scalingFactor), (int)(value.Height * scalingFactor), SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        get 
        {
            uint dpi = PInvoke.GetDpiForWindow(hWnd);
            double scalingFactor = dpi / 96.0;

            if (!PInvoke.GetWindowRect(hWnd, out RECT lpRect))
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            return new Size((lpRect.right - lpRect.left) / scalingFactor, (lpRect.bottom - lpRect.top) / scalingFactor);
        }
    }

    protected void CenterInPrimaryDisplay()
    {
        if (!PInvoke.GetWindowRect(hWnd, out RECT lpRect))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        DisplayArea primary = DisplayArea.Primary;

        int top = (primary.WorkArea.Height - (lpRect.bottom - lpRect.top)) / 2;
        int left = (primary.WorkArea.Width - (lpRect.right - lpRect.left)) / 2;

        top = Math.Max(top, 0); // guarantee the title bar is visible
        left = Math.Max(left, 0);

        if (!PInvoke.SetWindowPos(hWnd, (HWND)IntPtr.Zero, left, top, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    protected WINDOWPLACEMENT GetWindowPlacement()
    {
        WINDOWPLACEMENT placement = default;

        if (!PInvoke.GetWindowPlacement(hWnd, ref placement))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

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
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }
}
