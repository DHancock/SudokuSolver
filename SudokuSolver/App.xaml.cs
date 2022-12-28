using Sudoku.Utilities;
using Sudoku.ViewModels;
using Sudoku.Views;

// not to be confused with Windows.System.DispatcherQueue
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

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

            if (Settings.Data.RegisterFileTypes && !IsPackaged())
            {
                // registering file types causes all the desktop icons to be reset, only do it once, if possible 
                Settings.Data.RegisterFileTypes = false;

                string[] fileTypes = new[] { cFileExt };
                string[] verbs = new[] { "view", "edit" };
                
                // The icon is used in the explorer context menu "Open with" for this app's entry
                string logo = $"{Environment.ProcessPath},{cIconResourceID}";
                
                // This doesn't update .sdku file's icon to the app's icon, but does reset the icon to a
                // default file icon, replacing any previous "opens with" associations
                ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, cDisplayName, verbs, string.Empty);
            }
        }

        InitializeComponent();
    }

    // Invoked on the ui thread when the application is launched normally
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        AppActivationArguments args = appInstance.GetActivatedEventArgs();

        if (appInstance.IsCurrent || IsPackaged())
        {
            if (args.Kind == ExtendedActivationKind.File)
            {
                ProcessFileActivation(args);
            }
            else if (args.Kind == ExtendedActivationKind.Launch)
            {
                await ProcessCommandLine(Environment.GetCommandLineArgs());
            }
        }
        else
        {
            // single instancing only seems to work for unpackaged apps
            await appInstance.RedirectActivationToAsync(args);
            Process.GetCurrentProcess().Kill();
        }
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

    private static bool IsPackaged()
    {
        uint length = 0;
        WIN32_ERROR error = PInvoke.GetCurrentPackageFullName(ref length, null);
        return error == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER;
    }
}