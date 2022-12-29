using Sudoku.Utilities;

namespace Sudoku.Views;

public sealed partial class AboutBox : ContentDialog
{
    public AboutBox(XamlRoot xamlRoot, ElementTheme actualTheme)
    {
        this.InitializeComponent();

        XamlRoot = xamlRoot;
        RequestedTheme = actualTheme;
        VersionTextBlock.Text = string.Format(VersionTextBlock.Text, typeof(App).Assembly.GetName().Version);

#if DEBUG
        if (App.IsPackaged())
            VersionTextBlock.Text += " (P)";
#endif
        Loaded += async (s, e) =>
        {
            string path = ActualTheme == ElementTheme.Light ? "Sudoku.Resources.about_light.png" : "Sudoku.Resources.about_dark.png";
            AboutImage.Source = await Utils.LoadEmbeddedImageResource(path);
        };
    }
}
