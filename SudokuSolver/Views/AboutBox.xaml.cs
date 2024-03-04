namespace SudokuSolver.Views;

public sealed partial class AboutBox : UserControl
{
    public AboutBox()
    {
        this.InitializeComponent();

        AboutImage.Loaded += AboutImage_Loaded;
        AboutImage.ActualThemeChanged += AboutImage_ActualThemeChanged;

        VersionTextBlock.Text = $"Version: {Path.GetFileNameWithoutExtension(typeof(App).Assembly.GetName().Version?.ToString())}";
    }

    private void AboutImage_ActualThemeChanged(FrameworkElement sender, object args)
    {
        LoadImage();
    }

    private void AboutImage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadImage();
        AboutImage.Loaded -= AboutImage_Loaded;
    }

    private void LoadImage()
    {
        string fileName = AboutImage.ActualTheme == ElementTheme.Light ? "about_light.png" : "about_dark.png";
        AboutImage.Source = new BitmapImage(new Uri("ms-appx:///Resources/" + fileName));
    }
}
