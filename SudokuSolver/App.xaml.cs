using SudokuSolver.Utilities;
using SudokuSolver.Views;
using SudokuSolver.ViewModels;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public const string cFileExt = ".sdku";
    public const string cAppDisplayName = "Sudoku Solver";
    public static App Instance => (App)Current;

    private readonly DispatcherQueue uiThreadDispatcher;
    private readonly AppInstance appInstance;
    private readonly List<MainWindow> windowList = new();
    private MainWindow? currentWindow;
    internal ResourceLoader ResourceLoader { get; } = new ResourceLoader();
    internal SessionHelper SessionHelper { get; } = new SessionHelper();
    internal ClipboardHelper ClipboardHelper { get; } = new ClipboardHelper();
    private bool appClosing = false;

    private readonly SafeHandle localMutex;
    private readonly SafeHandle globalMutex;

    /// <summary>
    /// Initializes the singleton application object. This will be the single current
    /// instance, attempts to open more apps will already have been redirected.
    /// </summary>
    public App(AppInstance instance)
    {
        Debug.Assert(instance.IsCurrent);

        // Create the installer mutexes with current user access. The app is installed per
        // user rather than all users.
        const string name = "51ECE64E-1954-41C4-81FB-E3A60CE4C224";
        localMutex = PInvoke.CreateMutex(null, false, name);
        globalMutex = PInvoke.CreateMutex(null, false, "Global\\" + name);

        InitializeComponent();

        uiThreadDispatcher = DispatcherQueue.GetForCurrentThread();

        appInstance = instance;
        appInstance.Activated += MainInstance_Activated;
    }

    // Invoked on the ui thread when the application is launched normally
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        if (Settings.Instance.SaveSessionState)
        {
            await SessionHelper.LoadPreviousSessionAsync();

            if (Settings.Instance.OneTimeSaveOnEndSession)
            {
                Settings.Instance.OneTimeSaveOnEndSession = false;
                Settings.Instance.SaveSessionState = false;
            }
        }

        AppActivationArguments args = appInstance.GetActivatedEventArgs();

        if (args.Kind == ExtendedActivationKind.File)
        {
            ProcessFileActivation(args);
        }
        else if (args.Kind == ExtendedActivationKind.Launch)
        {
            await ProcessCommandLineAsync(Environment.GetCommandLineArgs());
        }

        currentWindow ??= CreateDefaultWindow();
        currentWindow.Activate();
        currentWindow.AttemptSwitchToForeground();
    }

    // Invoked when a redirection request is received.
    // Unlike OnLaunched(), this isn't called on the ui thread.
    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        if (!appClosing)
        {
            bool success = uiThreadDispatcher.TryEnqueue(async () =>
            {
                if (!appClosing)
                {
                    if (IsContentDialogOpen)
                    {
                        Utils.PlayExclamation();
                        currentWindow?.AttemptSwitchToForeground();
                        return;
                    }

                    if (e.Kind == ExtendedActivationKind.File)
                    {
                        ProcessFileActivation(e);
                    }
                    else if (e.Kind == ExtendedActivationKind.Launch)
                    {
                        await ProcessCommandLineAsync(SplitLaunchArguments(((ILaunchActivatedEventArgs)e.Data).Arguments));
                    }

                    if (!appClosing)
                    {
                        currentWindow?.Activate();
                        currentWindow?.AttemptSwitchToForeground();
                    }
                }
            });

            Debug.Assert(success);
        }
    }

    // can be called from both normal launch and redirection
    private void ProcessFileActivation(AppActivationArguments args)
    {
        if ((args.Data is IFileActivatedEventArgs fileData) && (fileData.Files.Count > 0))
        {
            foreach (IStorageItem storageItem in fileData.Files)
            {
                if (storageItem is StorageFile file)
                {
                    ProcessStorageFile(file);
                }
            }
        }
    }

    private async Task ProcessCommandLineAsync(IReadOnlyList<string> args)
    {
        // args[0] is typically the path to the executing assembly
        for (int index = 1; (index < args.Count) && !appClosing; index++)
        {
            string arg = args[index];

            if (IsValidFileExtension(arg) && File.Exists(arg))
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(arg);

                if (!appClosing)
                {
                    ProcessStorageFile(file);
                }
            }
        }
    }

    private void ProcessStorageFile(StorageFile file)
    {
        // Deliberately only check the current window, as does Notepad. Switching windows would be confusing.
        if ((currentWindow is not null) && currentWindow.IsOpenInExistingTab(file.Path))
        {
            currentWindow.SwitchToTab(file.Path);
        }
        else
        {
            currentWindow ??= new MainWindow(Settings.Instance.WindowState, Settings.Instance.RestoreBounds);
            currentWindow.AddTab(new PuzzleTabViewItem(currentWindow, file.Path));
        }
    }

    private static MainWindow CreateDefaultWindow()
    {
        MainWindow window = new MainWindow(Settings.Instance.WindowState, Settings.Instance.RestoreBounds);
        window.AddTab(new PuzzleTabViewItem(window));
        return window;
    }

    internal MainWindow? GetWindowForElement(UIElement element)
    {
        return windowList.FirstOrDefault(window => window.Content.XamlRoot == element.XamlRoot);
    }

    internal void RegisterWindow(MainWindow window)
    {
        Debug.Assert(!appClosing);
        windowList.Add(window);
        currentWindow = window;
    }

    internal void UnRegisterWindow(MainWindow window)
    {
        bool found = windowList.Remove(window);
        Debug.Assert(found);

        if (windowList.Count == 0)
        {
            appClosing = true;
            currentWindow = null;
        }
        else if (ReferenceEquals(currentWindow, window))
        {
            // If all the other windows are minimized then another window won't be
            // automatically activated. Until it's known, use the last one opened.
            currentWindow = windowList.LastOrDefault();
        }
    }

    public int WindowCount => windowList.Count;

    public void AttemptCloseAllWindows()
    {
        List<MainWindow> windows;

        if (Settings.Instance.SaveSessionState)
        {
            // indicates that all windows are to be saved by the session helper
            SessionHelper.IsExit = true;
            windows = GetWindowsInAscendingZOrder();
        }
        else
        {
            // the user will be prompted to save any unsaved changes
            windows = GetWindowsInDescendingZOrder();
        }

        foreach (MainWindow window in windows)
        {
            window.PostCloseMessage();
        }
    }

    internal RectInt32 GetNewWindowPosition(MainWindow newWindow, RectInt32 restoreBounds)
    {
        if (restoreBounds.IsEmpty())  // first run
        {
            return CenterInPrimaryDisplay(newWindow);
        }

        return GetNewWindowPosition(restoreBounds);
    }

    private static RectInt32 CenterInPrimaryDisplay(MainWindow window)
    {
        int width = window.ConvertToPixels(MainWindow.cInitialWidth);
        int height = window.ConvertToPixels(MainWindow.cInitialHeight);

        RectInt32 windowArea;
        RectInt32 workArea = DisplayArea.Primary.WorkArea;

        windowArea.X = Math.Max((workArea.Width - width) / 2, workArea.X);
        windowArea.Y = Math.Max((workArea.Height - height) / 2, workArea.Y);
        windowArea.Width = width;
        windowArea.Height = height;

        return windowArea;
    }

    private RectInt32 GetNewWindowPosition(RectInt32 bounds)
    {
        static bool TitleBarOverlaps(PointInt32 a, PointInt32 b, int titleBarHeight)
        {
            RectInt32 aRect = new RectInt32(a.X, a.Y, titleBarHeight, titleBarHeight);
            RectInt32 bRect = new RectInt32(b.X, b.Y, titleBarHeight, titleBarHeight);
            return aRect.Intersects(bRect);
        }

        int index = 0;
        int resetCount = 0;
        PointInt32 newPos = bounds.TopLeft();

        while ((index < windowList.Count) && (resetCount < windowList.Count))
        {
            MainWindow window = windowList[index++];

            PointInt32 existingPos = window.RestoreBounds.TopLeft();
            int titleBarHeight = window.AppWindow.TitleBar.Height;

            newPos = AdjustWindowBoundsForDisplay(new RectInt32(newPos.X, newPos.Y, bounds.Width, bounds.Height)).TopLeft();

            if (TitleBarOverlaps(existingPos, newPos, titleBarHeight))
            {
                newPos = existingPos.Offset(titleBarHeight + 1);
                index = 0;
                ++resetCount;  // avoid an infinite loop if the position cannot be adjusted due to display limits
            }
        }

        return AdjustWindowBoundsForDisplay(new RectInt32(newPos.X, newPos.Y, bounds.Width, bounds.Height));
    }

    private static RectInt32 AdjustWindowBoundsForDisplay(RectInt32 bounds)
    {
        Debug.Assert(!bounds.IsEmpty());

        RectInt32 workArea = DisplayArea.GetFromRect(bounds, DisplayAreaFallback.Nearest).WorkArea;
        PointInt32 position = bounds.TopLeft();

        if ((position.Y + bounds.Height) > workArea.Bottom())
        {
            position.Y = workArea.Bottom() - bounds.Height;
        }

        if (position.Y < workArea.Y)
        {
            position.Y = workArea.Y;
        }

        if ((position.X + bounds.Width) > workArea.Right())
        {
            position.X = workArea.Right() - bounds.Width;
        }

        if (position.X < workArea.X)
        {
            position.X = workArea.X;
        }

        int width = Math.Min(bounds.Width, workArea.Width);
        int height = Math.Min(bounds.Height, workArea.Height);

        return new RectInt32(position.X, position.Y, width, height);
    }

    // The command line is constructed by the os when a file is dragged 
    // and dropped onto the exe (or it's shortcut), so really should be well formed.
    private static List<string> SplitLaunchArguments(string commandLine)
    {
        List<string> arguments = new List<string>();
        StringBuilder sb = new StringBuilder();
        bool insideQuotes = false;

        foreach (char letter in commandLine)
        {
            if (letter == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (insideQuotes || (letter != ' '))
            {
                sb.Append(letter);
            }
            else if (sb.Length > 0)
            {
                arguments.Add(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            arguments.Add(sb.ToString());
        }

        return arguments;
    }

    public void RecordWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        // used to determine which window to activate, or add tabs too on launch redirection
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            currentWindow = (MainWindow)sender;
        }
    }

    public static string GetAppDataPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Join(localAppData, "sudokusolver.davidhancock.net.v2");
    }

    private bool IsContentDialogOpen => currentWindow is not null && currentWindow.ContentDialogHelper.IsContentDialogOpen;

    private static bool IsValidFileExtension(string path)
    {
        return string.Equals(cFileExt, Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
    }

    public void SaveStateOnEndSession()
    {
        if (!SessionHelper.IsEndSession)
        {
            SessionHelper.IsEndSession = true;

            if (!Settings.Instance.SaveSessionState)
            {
                // temporarily switch on, avoids the need to interrupt the shut down or sign out 
                Settings.Instance.SaveSessionState = true;
                Settings.Instance.OneTimeSaveOnEndSession = true;
            }

            foreach (MainWindow window in GetWindowsInAscendingZOrder())
            {
                SessionHelper.AddWindow(window);
            }

            // convert to synchronous, the window subclass proc cannot be async
            ManualResetEventSlim mres = new();

            Task.Run(async () =>
            {
                await Task.WhenAll(Settings.Instance.SaveAsync(), SessionHelper.SaveAsync());
                mres.Set();
            });

            mres.Wait();
        }
    }

    private List<MainWindow> GetWindowsInAscendingZOrder()
    {
        List<MainWindow> list = GetWindowsInDescendingZOrder();
        list.Reverse();
        return list;
    }

    private List<MainWindow> GetWindowsInDescendingZOrder()
    {
        List<MainWindow> list = new List<MainWindow>(windowList.Count);

        PInvoke.EnumWindows((HWND hWnd, LPARAM param) =>
        {
            foreach (MainWindow window in windowList)
            {
                if (window.WindowHandle == hWnd)
                {
                    list.Add(window);
                    break;
                }
            }

            return list.Count != windowList.Count;
        },
        (LPARAM)0);

        return list;
    }

    public void UpdateTheme()
    {
        // avoid using x:Bind in the window xaml file due to memory leaks
        // https://github.com/microsoft/microsoft-ui-xaml/issues/9960

        foreach (MainWindow window in windowList)
        {
            window.UpdateTheme();
        }
    }
}
