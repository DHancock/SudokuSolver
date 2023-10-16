using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsWindow : WindowBase
{
    public SettingsWindow()
    {
        this.InitializeComponent();

        this.SystemBackdrop = new MicaBackdrop();

        AppWindow.SetIcon("Resources\\app.ico");

        AppWindow.MoveAndResize(App.GetSettingsWindowPosition(this));

        string title = App.cDisplayName + " Settings";

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            CustomTitleBar.Title = title;
            CustomTitleBar.ParentAppWindow = AppWindow;
            CustomTitleBar.UpdateThemeAndTransparency(Settings.Data.ViewSettings.Theme);
            Activated += CustomTitleBar.ParentWindow_Activated;
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
        else
        {
            Title = title;
            CustomTitleBar.Visibility = Visibility.Collapsed;
        }

        // the app window's title is used in the task switcher
        AppWindow.Title = title;

        AppWindow.Closing += async (s, e) =>
        {
            bool lastWindow = App.Instance.UnRegisterWindow(this);

            // record now, a puzzle window could be the last window
            Settings.Data.SettingsRestoreBounds = RestoreBounds;

            AppWindow.Hide();

            if (lastWindow)
                await Settings.Data.Save();
        };
    }


    public Color UserColor = Colors.Red;
    public Color CalculatedColor = Colors.Green;

    public void UserColorChangedEventHandler(SimpleColorPicker sender, Color newColor)
    {
        UserColor = newColor;
    }
    public void CalculatedColorChangedEventHandler(SimpleColorPicker sender, Color newColor)
    {
        CalculatedColor = newColor;
    }


    public void ButtonClick(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)this.Content).RequestedTheme = ElementTheme.Dark;
    }
}
