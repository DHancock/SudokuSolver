//#define TEST_FILE_ACTIVATION

using SudokuSolver.Utilities;
using SudokuSolver.Views;

// not to be confused with Windows.System.DispatcherQueue
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

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

    public static bool IsPackaged { get; } = GetIsPackaged();

    private readonly DispatcherQueue uiThreadDispatcher;
    private readonly AppInstance appInstance;
    private readonly List<MainWindow> windowList = new List<MainWindow>();

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        appInstance = AppInstance.FindOrRegisterForKey(cAppKey);
        uiThreadDispatcher = DispatcherQueue.GetForCurrentThread();

        if (appInstance.IsCurrent)
        {
            appInstance.Activated += MainInstance_Activated;

#if DEBUG && TEST_FILE_ACTIVATION
            if (!IsPackaged)
            {
                // for testing only...
                // registration will be actioned from the installer, and then removed on uninstall
                // multiple registrations at different paths can cause errors, especially if one
                // of the apps has subsequently been deleted without unregistering...
                // file type activation for packaged apps is defined in the Package.appxmanifest

                RegisterFileTypeActivation();
            }
#endif
        }

        InitializeComponent();
    }

#if DEBUG && TEST_FILE_ACTIVATION
    private static void RegisterFileTypeActivation()
    {
        string[] fileTypes = new[] { cFileExt };
        string[] verbs = new[] { "open" };

        // the icon to use for .sdku files
        string logo = $"{Environment.ProcessPath},0";

        ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, cDisplayName, verbs, string.Empty);

        /*
        Registration creates the usual file extension association registry entries:

        HKCU\Software\Classes\.sdku\OpenWithProgids
        
        generating a prod id key value in the form of "App.xxxxxxxxxxxxxxxx.File" and an entry with that key
        that identifies the verbs and associated application path:

        HKCU\Software\Classes\App.xxxxxxxxxxxxxxxx.File

        however, it also creates another entry with that key, but minus the ".File" that lists the file extension 
        associations for that key under: 

        HKCU\Software\Microsoft\WindowsAppRuntimeApplications\App.xxxxxxxxxxxxxxxx

        It also adds it as a named value, along with other packaged AppX entries under:

        HKCU\Software\RegisteredApplications
        */
    }

    private void UnregisterFileTypeActivation()
    {
        try
        {
            string[] fileTypes = new[] { cFileExt };
            ActivationRegistrationManager.UnregisterForFileTypeActivation(fileTypes, Environment.ProcessPath);
        }
        catch (Exception ex)
        {
            // usually means the file types haven't been registered for the exe at the supplied path
            Debug.WriteLine(ex.ToString());
        }
    }
#endif

    // Invoked on the ui thread when the application is launched normally
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
#if DEBUG && TEST_FILE_ACTIVATION
        AppActivationArguments args = appInstance.GetActivatedEventArgs();

        if (appInstance.IsCurrent)
        {
            if (args.Kind == ExtendedActivationKind.File)
            {
                ProcessFileActivation(args);
            }
            else if (args.Kind == ExtendedActivationKind.Launch)
            {
                if (IsPackaged)
                    CreateNewWindow(storageFile: null);
                else
                    await ProcessCommandLine(Environment.GetCommandLineArgs());
            }
        }
        else
        {
            await appInstance.RedirectActivationToAsync(args);
            Process.GetCurrentProcess().Kill();
        }
#else
        if (IsPackaged)
            CreateNewWindow(storageFile: null);
        else
            await ProcessCommandLine(Environment.GetCommandLineArgs());
#endif
    }

    // Invoked when a redirection request is received.
    // Unlike OnLaunched(), this isn't called on the ui thread.
    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        if (e.Kind == ExtendedActivationKind.File)
        {
            bool success = uiThreadDispatcher.TryEnqueue(() =>
            {
                ProcessFileActivation(e);
            });

            Debug.Assert(success);
        }
    }

    private void ProcessFileActivation(AppActivationArguments args)
    {
        if ((args.Data is IFileActivatedEventArgs fileData) && (fileData.Files.Count > 0))
        {
            foreach (IStorageItem storageItem in fileData.Files)
            {
                if (storageItem is StorageFile storageFile)
                    CreateNewWindow(storageFile);
            }
        }
    }

    private async Task ProcessCommandLine(string[]? args)
    {
        if (args?.Length > 1)  // args[0] is typically the path to the executing assembly
        {
            for (int index = 1; index < args.Length; index++)
            {
                string arg = args[index];

                if (!string.IsNullOrEmpty(arg) && Path.GetExtension(arg).ToLower() == App.cFileExt && File.Exists(arg))
                {
                    StorageFile storgeFile = await StorageFile.GetFileFromPathAsync(arg);
                    CreateNewWindow(storgeFile);
                }
            }
        }
        else
            CreateNewWindow(storageFile: null);
    }

    internal void CreateNewWindow(StorageFile? storageFile, MainWindow? creator = null)
    {
        MainWindow window = new MainWindow(storageFile, creator);
        windowList.Add(window);
        TryBumpWindowToFront(window);
    }

    private static bool TryBumpWindowToFront(Window window)
    {
        HWND foreground = PInvoke.GetForegroundWindow();
        HWND target = (HWND)WindowNative.GetWindowHandle(window);

        if (target != foreground)
        {
            if (PInvoke.SetForegroundWindow(target))
                return true;

            Debug.WriteLine("SetForegroundWindow() was refused");
            return false;
        }

        return true;
    }

    internal bool UnRegisterWindow(MainWindow window)
    {
        bool found = windowList.Remove(window);
        Debug.Assert(found);

#if DEBUG && TEST_FILE_ACTIVATION
        if ((windowList.Count == 0) && !IsPackaged)
            UnregisterFileTypeActivation();
#endif
        return windowList.Count == 0;
    }

    internal PointInt32 AdjustPositionForOtherWindows(PointInt32 pos)
    {
        static bool TitleBarOverlaps(PointInt32 a, PointInt32 b, int titleBarHeight)
        {
            RectInt32 aRect = new RectInt32(a.X, a.Y, titleBarHeight, titleBarHeight);
            RectInt32 bRect = new RectInt32(b.X, b.Y, titleBarHeight, titleBarHeight);
            return aRect.Intersects(bRect); 
        }

        const int cTitleBarHeight = 32;
        int index = 0;

        while (index < windowList.Count)
        {
            MainWindow existingWindow = windowList[index++];
            PointInt32 existingPos = existingWindow.RestoreBounds.TopLeft();
            double scaleFactor = existingWindow.GetScaleFactor();

            int clientTitleBarHeight = MainWindow.ConvertToDeviceSize(cTitleBarHeight, scaleFactor);

            if (TitleBarOverlaps(existingPos, pos, clientTitleBarHeight))
            {
                pos = existingPos.Offset(clientTitleBarHeight + 1);
                index = 0;
            }
        }

        return pos;
    }

    private static bool GetIsPackaged()
    {
        uint length = 0;
        WIN32_ERROR error = PInvoke.GetCurrentPackageFullName(ref length, null);
        return error == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER;
    }
}