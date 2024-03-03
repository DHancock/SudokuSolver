namespace SudokuSolver.Views;

public sealed partial class AboutBox : UserControl
{
    public AboutBox()
    {
        this.InitializeComponent();

        AboutImage.Loaded += (s, e) => LoadImage();
        AboutImage.ActualThemeChanged += (s, e) => LoadImage();

        VersionTextBlock.Text = $"Version: {Path.GetFileNameWithoutExtension(typeof(App).Assembly.GetName().Version?.ToString())}";
    }

    private void LoadImage()
    {
        string fileName = AboutImage.ActualTheme == ElementTheme.Light ? "about_light.png" : "about_dark.png";
        AboutImage.Source = new BitmapImage(new Uri("ms-appx:///Resources/" + fileName));
    }
}
