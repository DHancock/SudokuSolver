using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsWindow : WindowBase
{
    public SettingsWindow()
    {
        this.InitializeComponent();

        OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter ;

        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsResizable = true;


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
            AppWindow.Hide();

            Settings.Data.SettingsRestoreBounds = RestoreBounds;
            bool lastWindow = App.Instance.UnRegisterWindow(this);
            
            if (lastWindow)
                await Settings.Data.Save();
        };
    }
}
