using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

public sealed partial class AboutBox : ContentDialog
{
    public AboutBox(XamlRoot xamlRoot)
    {
        this.InitializeComponent();

        XamlRoot = xamlRoot;
        Version? v = typeof(App).Assembly.GetName().Version;

        if (v is not null)
            VersionTextBlock.Text = string.Format(VersionTextBlock.Text, v.Major, v.Minor, v.Build);

        PrimaryButtonText = "OK";

#if DEBUG
        if (App.IsPackaged)
            VersionTextBlock.Text += " (P)";
        else
            VersionTextBlock.Text += " (D)";
#endif

        Loaded += (s, e) =>
        {
            string fileName = ActualTheme == ElementTheme.Light ? "about_light.png" : "about_dark.png";
            AboutImage.Source = new BitmapImage(new Uri("ms-appx:///Resources/" + fileName));
        };
    }
}
