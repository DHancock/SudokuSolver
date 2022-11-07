namespace Sudoku.Views;

public sealed partial class CustomTitleBar : UserControl
{
    public AppWindow? AppWindow { set; private get; }
    public double ScaleFactor { set; private get; }

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
                Debug.Assert(AppWindow is not null);
                Debug.Assert(ScaleFactor > 0.0);

                LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / ScaleFactor);
                RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / ScaleFactor);
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
        Debug.Assert(AppWindow is not null);
        Debug.Assert(AppWindow.TitleBar is not null);

        AppWindowTitleBar titleBar = AppWindow.TitleBar;

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
}
