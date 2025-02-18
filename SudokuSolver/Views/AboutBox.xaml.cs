using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class AboutBox : UserControl
{
    public AboutBox()
    {
        this.InitializeComponent();

        AboutImage.Loaded += AboutImage_Loaded;
        AboutImage.ActualThemeChanged += AboutImage_ActualThemeChanged;

        string template = App.Instance.ResourceLoader.GetString("VersionTemplate");
        string? version = Path.GetFileNameWithoutExtension(typeof(App).Assembly.GetName().Version?.ToString());

        VersionTextBlock.Text = string.Format(template, version);
        AppNameTextBlock.Text = App.cAppDisplayName;

        // Use the Tag to identify that this text block contains a hyperlink. Work around for:
        // https://github.com/microsoft/WindowsAppSDK/issues/4722
        HyperlinkTextBlock.Tag = HyperlinkTextBlock;
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
        AboutImage.Source = GetImage(AboutImage.ActualTheme);
    }

    public static BitmapImage GetImage(ElementTheme theme)
    {
        string fileName = Utils.NormaliseTheme(theme) == ElementTheme.Light ? "about_light.png" : "about_dark.png";
        return new BitmapImage(new Uri("ms-appx:///Resources/" + fileName));
    }
}
