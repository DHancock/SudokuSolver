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
    public const string cDisplayName = "Sudoku Solver";
    public const string cNewPuzzleName = "Untitled";
    public static App Instance => (App)Current;

    private readonly DispatcherQueue uiThreadDispatcher;
    private readonly AppInstance appInstance;
    private readonly List<MainWindow> windowList = new();
    private MainWindow? currentWindow;

    private bool appClosing = false;

    /// <summary>
    /// Initializes the singleton application object. This will be the single current
    /// instance, attempts to open more apps will already have been redirected.
    /// </summary>
    public App(AppInstance instance)
    {
        InitializeComponent();

        uiThreadDispatcher = DispatcherQueue.GetForCurrentThread();

        Debug.Assert(instance.IsCurrent);
        appInstance = instance;
        appInstance.Activated += MainInstance_Activated;
    }

    // Invoked on the ui thread when the application is launched normally
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        AppActivationArguments args = appInstance.GetActivatedEventArgs();

        if (args.Kind == ExtendedActivationKind.File)
        {
            ProcessFileActivation(args);
        }
        else if (args.Kind == ExtendedActivationKind.Launch)
        {
            string[] commandLine = Environment.GetCommandLineArgs();

            bool windowCreated = await ProcessCommandLine(commandLine);

            if (!windowCreated)
            {
                MainWindow window = new MainWindow(Settings.Data.WindowState, Settings.Data.RestoreBounds);
                window.AttemptSwitchToForeground();
            }
        }
    }

    // Invoked when a redirection request is received.
    // Unlike OnLaunched(), this isn't called on the ui thread.
    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        bool success = uiThreadDispatcher.TryEnqueue(async () =>
        {
            if (!appClosing)
            {
                if (e.Kind == ExtendedActivationKind.File)
                {
                    ProcessFileActivation(e);
                }
                else if (e.Kind == ExtendedActivationKind.Launch)
                {
                    await ProcessRedirectedLaunchActivation(e);
                }
            }
        });

        Debug.Assert(success);
    }


    // can be called from both normal launch and redirrection
    private void ProcessFileActivation(AppActivationArguments args)
    {
        if ((args.Data is IFileActivatedEventArgs fileData) && (fileData.Files.Count > 0) && !appClosing)
        {
            foreach (IStorageItem storageItem in fileData.Files)
            {
                if (storageItem is StorageFile storageFile)
                {
                    if (currentWindow is null)
                    {
                        currentWindow = new MainWindow(Settings.Data.WindowState, Settings.Data.RestoreBounds, storageFile);
                    }
                    else
                    {
                        TabViewItem newTab = new PuzzleTabViewItem(currentWindow, storageFile);
                        currentWindow.AddTab(newTab);
                    }
                }
            }

            currentWindow?.AttemptSwitchToForeground();
        }
    }


    private async Task ProcessRedirectedLaunchActivation(AppActivationArguments args)
    {
        if (args.Data is ILaunchActivatedEventArgs launchData)
        {
            List<string> commandLine = SplitLaunchActivationCommandLine(launchData.Arguments);

            bool windowCreated = await ProcessCommandLine(commandLine);

            if (!windowCreated)
            {
                currentWindow?.AttemptSwitchToForeground();
            }
        }
    }

    private async Task<bool> ProcessCommandLine(IReadOnlyList<string> args)
    {
        bool actioned = false;

        // args[0] is typically the path to the executing assembly
        for (int index = 1; index < args.Count; index++)
        {
            string arg = args[index];

            if (cFileExt.Equals(Path.GetExtension(arg), StringComparison.OrdinalIgnoreCase) && File.Exists(arg))
            {
                StorageFile storageFile = await StorageFile.GetFileFromPathAsync(arg);

                if (currentWindow is null)
                {
                    currentWindow = new MainWindow(Settings.Data.WindowState, Settings.Data.RestoreBounds, storageFile);
                }
                else
                {
                    TabViewItem newTab = new PuzzleTabViewItem(currentWindow, storageFile);
                    currentWindow.AddTab(newTab);
                }

                actioned = true;
                currentWindow?.AttemptSwitchToForeground();
            }
        }

        return actioned;
    }

    internal MainWindow? GetWindowForElement(UIElement element)
    {
        foreach (MainWindow window in windowList)
        {
            if (window.Content.XamlRoot == element.XamlRoot)
            {
                return window;
            }
        }

        Debug.Fail($"{nameof(GetWindowForElement)} returns null");
        return null;
    }

    internal void RegisterWindow(MainWindow window)
    {
        Debug.Assert(!appClosing);
        windowList.Add(window);
        currentWindow = window;
    }

    internal bool UnRegisterWindow(MainWindow window)
    {
        appClosing = windowList.Count == 1;

        bool found = windowList.Remove(window);
        Debug.Assert(found);

        // If all the other windows are minimized then another window won't be
        // automatically activated. Until it's known, use the last one opened.
        if (ReferenceEquals(currentWindow, window))
        {
            currentWindow = windowList.LastOrDefault();
        }

        return appClosing;
    }

    public void AttemptCloseAllWindows()
    {
        foreach (WindowBase window in windowList)
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

    private static RectInt32 CenterInPrimaryDisplay(WindowBase window)
    {
        double scaleFactor = window.GetScaleFactor();
        int width = WindowBase.ConvertToDeviceSize(window.InitialWidth, scaleFactor);
        int height = WindowBase.ConvertToDeviceSize(window.InitialHeight, scaleFactor);

        RectInt32 windowArea;
        RectInt32 workArea = DisplayArea.Primary.WorkArea;

        windowArea.X = Math.Max((workArea.Width - width) / 2, workArea.X);
        windowArea.Y = Math.Max((workArea.Height - height) / 2, workArea.Y);
        windowArea.Width = width;
        windowArea.Height = height;

        return windowArea;
    }

    internal RectInt32 GetNewWindowPosition(RectInt32 bounds)
    {
        static bool TitleBarOverlaps(PointInt32 a, PointInt32 b, int titleBarHeight)
        {
            RectInt32 aRect = new RectInt32(a.X, a.Y, titleBarHeight, titleBarHeight);
            RectInt32 bRect = new RectInt32(b.X, b.Y, titleBarHeight, titleBarHeight);
            return aRect.Intersects(bRect);
        }

        const int cTitleBarHeight = 32;
        int index = 0;
        int resetCount = 0;
        PointInt32 newPos = bounds.TopLeft();

        while ((index < windowList.Count) && (resetCount < windowList.Count))
        {
            WindowBase existingWindow = windowList[index++];

            if (existingWindow is MainWindow)
            {
                PointInt32 existingPos = existingWindow.RestoreBounds.TopLeft();
                double scaleFactor = existingWindow.GetScaleFactor();
                int clientTitleBarHeight = WindowBase.ConvertToDeviceSize(cTitleBarHeight, scaleFactor);

                newPos = AdjustWindowBoundsForDisplay(new RectInt32(newPos.X, newPos.Y, bounds.Width, bounds.Height)).TopLeft();

                if (TitleBarOverlaps(existingPos, newPos, clientTitleBarHeight))
                {
                    newPos = existingPos.Offset(clientTitleBarHeight + 1);
                    index = 0;
                    ++resetCount;  // avoid an infinate loop if the position cannot be adjusted due to display limits
                }
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
    private static List<string> SplitLaunchActivationCommandLine(string commandLine)
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
}
