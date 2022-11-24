using Sudoku.Views;

namespace Sudoku;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public const string cFileExt = ".sdku";
    public const string cDisplayName = "Sudoku Solver";
    public const string cIconResourceID = "32512";

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        string[] fileTypes = new[]{ cFileExt };
        string[] verbs = new[]{ "view", "edit" };
        string logo = $"{Path.ChangeExtension(typeof(App).Assembly.Location, ".exe")},{cIconResourceID}";
            
        ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, cDisplayName, verbs, string.Empty);
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    public static Window? MainWindow { get => ((App)Current).m_window; }

    private Window? m_window;
}