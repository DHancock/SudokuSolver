namespace Sudoku.Views;

public sealed partial class CustomTitleBar : UserControl
{
    public AppWindow? ParentAppWindow { get; set; }
    private bool adjustPadding = true;

    public CustomTitleBar()
    {
        this.InitializeComponent();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            LoadWindowIconImage();

            RegisterPropertyChangedCallback(RequestedThemeProperty, ThemeChangedCallback);

            SizeChanged += (s, e) =>
            {
                if (adjustPadding)
                {
                    adjustPadding = false;

                    Debug.Assert(ParentAppWindow is not null);
                    double scaleFactor = PInvoke.GetDpiForWindow((HWND)Win32Interop.GetWindowFromWindowId(ParentAppWindow.Id)) / 96.0;

                    LeftPaddingColumn.Width = new GridLength(ParentAppWindow.TitleBar.LeftInset / scaleFactor);
                    RightPaddingColumn.Width = new GridLength(ParentAppWindow.TitleBar.RightInset / scaleFactor);
                }

                windowTitle.Width = Math.Max(e.NewSize.Width - (LeftPaddingColumn.Width.Value + IconColumn.Width.Value + RightPaddingColumn.Width.Value), 0);
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
        Debug.Assert(ParentAppWindow is not null);
        Debug.Assert(ParentAppWindow.TitleBar is not null);

        AppWindowTitleBar titleBar = ParentAppWindow.TitleBar;

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

    public void ParentWindow_DpiChanged(object sender, DpiChangedEventArgs args)
    {
        // the title bar insets haven't been updated yet, so just flag the change
        adjustPadding = true;
    }
}
