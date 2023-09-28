namespace SudokuSolver.Views;

internal abstract class WindowBase : Window
{
    public double MinWidth { get; set; }
    public double MinHeight { get; set; }
    public double InitialWidth { get; set; }
    public double InitialHeight { get; set; }
    public IntPtr WindowPtr { get; }

    private readonly SUBCLASSPROC subClassDelegate;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;

    public WindowBase()
    {
        WindowPtr = WindowNative.GetWindowHandle(this);
        
        // sub class to set a minimum window size
        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)WindowPtr, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        AppWindow.Changed += AppWindow_Changed;
        Activated += App.Instance.RecordWindowActivated;
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
        else if (uMsg == PInvoke.WM_SYSCOMMAND) // alt 
        {
            if (lParam == VK_SPACE) 
            {
                if (IsContentDialogOpen())
                {
                    // right click works, but not via the keyboard
                    return new LRESULT(0);
                }

                // shouldn't have two active menus
                CloseFlyouts();
            }
        }
        else if ((uMsg == PInvoke.WM_NCRBUTTONDOWN) && (wParam == HTCAPTION))
        {
            // only applicable if the custom title bar isn't used
            CloseFlyouts();
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    protected void CloseFlyouts()
    {
        if ((Content is not null) && (Content.XamlRoot is not null))
        {
            foreach (Popup popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(Content.XamlRoot))
            {
                if (popup.Child is not ContentDialog)
                    popup.IsOpen = false;
            }
        }
    }

    private bool IsContentDialogOpen()
    {
        if ((Content is not null) && (Content.XamlRoot is not null))
        {
            foreach (Popup popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(Content.XamlRoot))
            {
                if (popup.Child is ContentDialog)
                    return true;
            }
        }

        return false;
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
