namespace Sudoku.Views;

internal sealed partial class CustomTitleBar : UserControl
{
    private AppWindow? parentAppWindow;

    public CustomTitleBar()
    {
        this.InitializeComponent();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SizeChanged += (s, e) =>
            {
                Debug.Assert(ParentAppWindow is not null);
                double scaleFactor = Content.XamlRoot.RasterizationScale;

                LeftPaddingColumn.Width = new GridLength(ParentAppWindow.TitleBar.LeftInset / scaleFactor);
                RightPaddingColumn.Width = new GridLength(ParentAppWindow.TitleBar.RightInset / scaleFactor);

                windowTitle.Width = Math.Max(e.NewSize.Width - (LeftPaddingColumn.Width.Value + IconColumn.Width.Value + RightPaddingColumn.Width.Value), 0);
            };

            ActualThemeChanged += (s, a) =>
            {
                UpdateTitleBarCaptionButtons();
            };

            Loaded += async (s, a) =>
            {
                windowIcon.Source = await MainWindow.LoadEmbeddedImageResource("Sudoku.Resources.app.png");
            };
        }
    }

    public AppWindow? ParentAppWindow
    {
        get => parentAppWindow;
        set
        {
            Debug.Assert(value is not null);
            parentAppWindow = value;
            UpdateTitleBarCaptionButtons();
        }
    }

    public string Title
    {
        set => windowTitle.Text = value;
    }

    private void UpdateTitleBarCaptionButtons()
    {
        Debug.Assert(ParentAppWindow is not null);
        Debug.Assert(ParentAppWindow.TitleBar is not null);
        Debug.Assert(ActualTheme != ElementTheme.Default);

        AppWindowTitleBar titleBar = ParentAppWindow.TitleBar;

        titleBar.BackgroundColor = Colors.Transparent;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
        titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        
        if (ActualTheme == ElementTheme.Light)
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
}
