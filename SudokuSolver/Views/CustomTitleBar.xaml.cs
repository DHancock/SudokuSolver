namespace Sudoku.Views
{
    public sealed partial class CustomTitleBar : UserControl
    {
        public AppWindow? AppWindow { set; private get; }
        public double ScaleFactor { set; private get; }

        public CustomTitleBar()
        {
            this.InitializeComponent();
            
            LoadWindowIconImage();

            SizeChanged += (s, e) =>
            {
                Debug.Assert(AppWindow is not null);
                Debug.Assert(ScaleFactor > 0.0);

                LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / ScaleFactor);
                RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / ScaleFactor);

                windowTitle.Width = e.NewSize.Width - (LeftPaddingColumn.Width.Value + IconColumn.Width.Value + RightPaddingColumn.Width.Value);
            };
        }

        private async void LoadWindowIconImage()
        {
            windowIcon.Source = await MainWindow.LoadEmbeddedImageResource("Sudoku.Resources.app.png");
        }

        public string Title
        {
            set => windowTitle.Text = value;
        }

        // use method hiding to replace the base property
        public new static readonly DependencyProperty RequestedThemeProperty =
            DependencyProperty.Register(nameof(RequestedTheme),
                typeof(ElementTheme),
                typeof(CustomTitleBar),
                new PropertyMetadata(ElementTheme.Default, ThemeChangedCallback));

        public new ElementTheme RequestedTheme
        {
            get { return (ElementTheme)GetValue(RequestedThemeProperty); }
            set { base.SetValue(RequestedThemeProperty, value); }
        }

        private static void ThemeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                CustomTitleBar ctb = (CustomTitleBar)d;

                ctb.layoutRoot.RequestedTheme = (ElementTheme)e.NewValue;
                ctb.UpdateTitleBarCaptionButtons();
            }
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
    }
}
