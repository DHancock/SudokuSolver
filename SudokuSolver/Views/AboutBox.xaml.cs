namespace SudokuSolver.Views;

public sealed partial class AboutBox : UserControl
{
    public AboutBox()
    {
        this.InitializeComponent();

        AboutImage.Loaded += AboutImage_Loaded;
        AboutImage.ActualThemeChanged += AboutImage_ActualThemeChanged;

        string template = App.Instance.ResourceLoader.GetString("VersionTemplate");
        string? version = Path.GetFileNameWithoutExtension(typeof(App).Assembly.GetName().Version?.ToString());

        VersionTextBlock.Text = string.Format(template, version);
        AppNameTextBlock.Text = App.Instance.AppDisplayName;
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
