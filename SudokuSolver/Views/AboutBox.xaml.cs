using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

public sealed partial class AboutBox : ContentDialog
{
    public AboutBox(XamlRoot xamlRoot)
    {
        this.InitializeComponent();

        XamlRoot = xamlRoot;
        VersionTextBlock.Text = string.Format(VersionTextBlock.Text, typeof(App).Assembly.GetName().Version);
        PrimaryButtonText = "OK";

#if DEBUG
        if (App.IsPackaged)
            VersionTextBlock.Text += " (P)";
        else
            VersionTextBlock.Text += " (D)";
#endif

        Loaded += async (s, e) =>
        {
            string path = ActualTheme == ElementTheme.Light ? "SudokuSolver.Resources.about_light.png" : "SudokuSolver.Resources.about_dark.png";
            AboutImage.Source = await Utils.LoadEmbeddedImageResource(path);
        };
    }
}
