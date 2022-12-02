using Sudoku.ViewModels;
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
    public const string cNewPuzzleName = "Untitled";
    private const string cAppKey = "sudoku-app";

    private Microsoft.UI.Dispatching.DispatcherQueue? uiThreadDispatcher;
    private readonly AppInstance appInstance;
    private static readonly List<MainWindow> sWindowList = new();

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        appInstance = AppInstance.FindOrRegisterForKey(cAppKey);

        if (appInstance.IsCurrent)
        {
            appInstance.Activated += MainInstance_Activated;

            if (Settings.Data.RegisterFileTypes)
            {
                // registering file types causes all the desktop icons to be reset, only do it once, if possible 
                Settings.Data.RegisterFileTypes = false;

                string[] fileTypes = new[] { cFileExt };
                string[] verbs = new[] { "view", "edit" };

#if PACKAGED
                string logo = string.Empty;  // use default or specify a relative image path
#else
                string logo = $"{Path.ChangeExtension(typeof(App).Assembly.Location, ".exe")},{cIconResourceID}";
#endif
                ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, cDisplayName, verbs, string.Empty);
            }
        }
    }

    // Invoked on the ui thread when the application is launched normally
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        Interlocked.Exchange(ref uiThreadDispatcher, Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());

        AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (appInstance.IsCurrent)
        {
            if (args.Kind == ExtendedActivationKind.File)
            {
                ProcessFileActivation(args);
            }
            else if (args.Kind == ExtendedActivationKind.Launch)
            {
                await ProcessCommandLine();
            }
        }
        else
        {
            await appInstance.RedirectActivationToAsync(args);
            Process.GetCurrentProcess().Kill();
        }
    }

    // Invoked when a redirection request is received.
    // Unlike OnLaunched(), this isn't called on the ui thread.
    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        if (e.Kind == ExtendedActivationKind.File)
            ProcessFileActivation(e);
    }

    private void ProcessFileActivation(AppActivationArguments args)
    {
        if (uiThreadDispatcher is not null)
        {
            if ((args.Data is IFileActivatedEventArgs fileData) && (fileData.Files.Count > 0))
            {
                foreach (IStorageItem storageItem in fileData.Files)
                {
                    if (storageItem is StorageFile storageFile)
                    {
                        if (uiThreadDispatcher.HasThreadAccess)
                            CreateNewWindow(storageFile);
                        else
                        {
                            bool success = uiThreadDispatcher.TryEnqueue(() =>
                            {
                                CreateNewWindow(storageFile);
                            });

                            Debug.Assert(success);
                        }
                    }
                }
            }
        }
    }

    private async static Task ProcessCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        if (args?.Length > 1)  // args[0] is typically the path to the executing assembly
        {
            for (int index = 1; index < args.Length; index++)
            {
                string arg = args[index];

                if (!string.IsNullOrEmpty(arg) && Path.GetExtension(arg.ToLower()) == App.cFileExt && File.Exists(arg))
                {
                    StorageFile storgeFile = await StorageFile.GetFileFromPathAsync(arg);
                    CreateNewWindow(storgeFile);
                }
            }
        }
        else
            CreateNewWindow(null);
    }

    public static void CreateNewWindow(StorageFile? storageFile)
    {
        MainWindow window = new MainWindow(storageFile);
        sWindowList.Add(window);
        window.Activate();
    }

    internal static bool UnRegisterWindow(MainWindow window)
    {
        bool found = sWindowList.Remove(window);
        Debug.Assert(found);
        return sWindowList.Count == 0;
    }
}