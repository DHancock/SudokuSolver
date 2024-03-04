namespace SudokuSolver.Views;

public sealed partial class AboutBox : UserControl
{
    public AboutBox()
    {
        this.InitializeComponent();

        Loaded += AboutBox_Loaded;
        ActualThemeChanged += AboutBox_ActualThemeChanged;

        VersionTextBlock.Text = $"Version: {Path.GetFileNameWithoutExtension(typeof(App).Assembly.GetName().Version?.ToString())}";
    }

    private void AboutBox_ActualThemeChanged(FrameworkElement sender, object args)
    {
        AboutBox aboutBox = (AboutBox)sender;
        aboutBox.LoadImage();
    }

    private void AboutBox_Loaded(object sender, RoutedEventArgs e)
    {
        AboutBox aboutBox = (AboutBox)sender;
        aboutBox.LoadImage();
        aboutBox.Loaded -= AboutBox_Loaded;
    }

    private void LoadImage()
    {
        string fileName = AboutImage.ActualTheme == ElementTheme.Light ? "about_light.png" : "about_dark.png";
        AboutImage.Source = new BitmapImage(new Uri("ms-appx:///Resources/" + fileName));
    }
}
