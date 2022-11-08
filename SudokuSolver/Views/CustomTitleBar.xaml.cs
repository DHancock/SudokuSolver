namespace Sudoku.Views;

public sealed partial class CustomTitleBar : UserControl
{
    public AppWindow? ParentWindow { set; private get; }

    public CustomTitleBar()
    {
        this.InitializeComponent();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            LoadWindowIconImage();

            RegisterPropertyChangedCallback(RequestedThemeProperty, ThemeChangedCallback);

            SizeChanged += (s, e) =>
            {
                windowTitle.Width = Math.Max(e.NewSize.Width - (LeftPaddingColumn.Width.Value + IconColumn.Width.Value + RightPaddingColumn.Width.Value), 0);
            };

            Loaded += (s, e) =>
            {
                Debug.Assert(ParentWindow is not null);

                double scaleFactor = GetScaleFactor();

                LeftPaddingColumn.Width = new GridLength(ParentWindow.TitleBar.LeftInset / scaleFactor);
                RightPaddingColumn.Width = new GridLength(ParentWindow.TitleBar.RightInset / scaleFactor);
            };
        }
    }

    private async void LoadWindowIconImage()
    {
        windowIcon.Source = await MainWindow.LoadEmbeddedImageResource("Sudoku.Resources.app.png");
    }

    public string Title
    {
        set => windowTitle.Text = value;
    }

    private void ThemeChangedCallback(DependencyObject sender, DependencyProperty dp)
    {
        UpdateTitleBarCaptionButtons();
    }

    private void UpdateTitleBarCaptionButtons()
    {
        Debug.Assert(ParentWindow is not null);
        Debug.Assert(ParentWindow.TitleBar is not null);

        AppWindowTitleBar titleBar = ParentWindow.TitleBar;

        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
        titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        if (RequestedTheme == ElementTheme.Light)
        {
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonHoverBackgroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
        }
        else
        {
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = Colors.DimGray;
            titleBar.ButtonInactiveForegroundColor = Colors.DimGray;
        }
    }

    public void ParentWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            bool stateFound = VisualStateManager.GoToState(this, "Deactivated", false);
            Debug.Assert(stateFound);
        }
        else
        {
            bool stateFound = VisualStateManager.GoToState(this, "Activated", false);
            Debug.Assert(stateFound);
        }
    }

    private double GetScaleFactor()
    {
        Debug.Assert(ParentWindow is not null);
        uint dpi = PInvoke.GetDpiForWindow((HWND)Win32Interop.GetWindowFromWindowId(ParentWindow.Id));
        Debug.Assert(dpi > 0);
        return dpi / 96.0;
    }
}
