using Microsoft.UI.Dispatching;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

public static class Program
{
    private const string cInstallerMutexName = "sudukosolver.8628521D92E74106";
    private const string cAppKey = cInstallerMutexName;

    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [STAThread]
    static async Task<int> Main(string[] args)
    {
        XamlCheckProcessRequirements();
        ComWrappersSupport.InitializeComWrappers();

        if (args.Length == 1)
        {
            switch (args[0])
            {
                case "/register": RegisterFileTypeActivation(); return 1;
                case "/unregister": UnregisterFileTypeActivation(); return 2;
            }
        }

        AppInstance appInstance = AppInstance.FindOrRegisterForKey(cAppKey);

        // appInstance.IsCurrent isn't reliable, use the installer mutex instead
        if (Mutex.TryOpenExisting(cInstallerMutexName, out Mutex? mutex))
        {
            try
            {
                AppActivationArguments aea = AppInstance.GetCurrent().GetActivatedEventArgs();

                if (aea.Kind == ExtendedActivationKind.File)
                {
                    await appInstance.RedirectActivationToAsync(aea);
                    return 3;
                }
                else if (aea.Kind == ExtendedActivationKind.Launch)
                {
                    AttemptSwitchToMainWindow();
                    return 4;
                }

                Debug.Fail($"unknown activation kind: {aea.Kind}");
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }

            return 5;  // enforce single instancing
        }

        // the uninstaller uses this mutex to check if the app is running
        _ = new Mutex(initiallyOwned: false, cInstallerMutexName);

        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App(appInstance);
        });

        return 0;
    }

    private static void RegisterFileTypeActivation()
    {
        string[] fileTypes = new[] { App.cFileExt };
        string[] verbs = new[] { "open" };
        string logo = $"{Environment.ProcessPath},0";

        // multiple registrations for the same path won't create additional registry entries
        // however it will cause all desktop icons to be refreshed, so should be avoided
        ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, App.cDisplayName, verbs, string.Empty);
    }

    private static void UnregisterFileTypeActivation()
    {
        try
        {
            string[] fileTypes = new[] { App.cFileExt };
            ActivationRegistrationManager.UnregisterForFileTypeActivation(fileTypes, Environment.ProcessPath);
        }
        catch (Exception ex)
        {
            // a file not found exception probably means the file type hasn't been
            // registered for this application's path
            Debug.Fail(ex.ToString());
        }
    }

    /*
        As of WinAppSdk 1.2.4, registration creates most of the usual file extension association registry entries,
        generating a prod id key value in the form of "App.xxxxxxxxxxxxxxxx.File":

        HKCU\Software\Classes\.sdku\OpenWithProgids
        HKCU\Software\Classes\App.xxxxxxxxxxxxxxxx.File

        It doesn't create this usual file association entry containing the file extension:

        HKCU\Software\Classes\Applications\<app name.exe>\SupportedTypes -> name of .sdku

        It does create other entries with the prog id key, but minus the ".File" part. It lists the file extension 
        associations for that key under: 

        HKCU\Software\Microsoft\WindowsAppRuntimeApplications\App.xxxxxxxxxxxxxxxx

        It also adds it as a named value under:

        HKCU\Software\RegisteredApplications

        Neither of the last two are deleted on unregistration, but the .sdku name is removed from 
        the WindowsAppRuntimeApplications\App.xxxxxxxxxxxxxxxx\Capabilties\FileAssociations
    */

    private static bool AttemptSwitchToMainWindow()
    {
        Process current = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(current.ProcessName);

        foreach (Process process in processes)
        {
            try
            {
                if ((process.Id != current.Id) && PathEquals(process, current) && (process.MainWindowHandle != IntPtr.Zero))
                {
                    HWND hWnd = (HWND)process.MainWindowHandle;

                    WINDOWPLACEMENT placement = default;
                    placement.length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>();

                    if (!PInvoke.GetWindowPlacement(hWnd, ref placement))
                        throw new Win32Exception(Marshal.GetLastPInvokeError());

                    if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
                        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_RESTORE);

                    if (PInvoke.SetForegroundWindow(hWnd))
                        return true;

                    // restore window opening state (it doesn't change the windows visibility)
                    if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
                        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOWMINIMIZED);
                }
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        return false;
    }

    private static bool PathEquals(Process a, Process b)
    {
        return (a.MainModule is not null) &&
                (b.MainModule is not null) &&
                string.Equals(a.MainModule.FileName, b.MainModule.FileName, StringComparison.OrdinalIgnoreCase);
    }
}
