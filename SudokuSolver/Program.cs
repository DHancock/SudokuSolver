using Microsoft.UI.Dispatching;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

public static class Program
{
    private const string cAppKey = "586A28D4-3FF0-42B2-829B-5F02BBFC8352";

    [STAThread]
    static void Main(string[] args)
    {
        if ((args.Length == 1) && (args[0] == "/register"))
        {
            RegisterFileTypeActivation();
        }
        else if ((args.Length == 1) && (args[0] == "/unregister"))
        {
            DeleteAppData();
            UnregisterFileTypeActivation();
        }
        else
        {
            AppInstance appInstance = AppInstance.FindOrRegisterForKey(cAppKey);

            if (!appInstance.IsCurrent)
            {
                AppActivationArguments aea = AppInstance.GetCurrent().GetActivatedEventArgs();
                appInstance.RedirectActivationToAsync(aea).AsTask().Wait();
            }
            else
            {
                Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App(appInstance);
                });
            }
        }
    }

    private static void RegisterFileTypeActivation()
    {
        string[] verbs = ["open"];
        string[] fileTypes = [App.cFileExt];
        string logo = $"{Environment.ProcessPath},0";

        try
        {
            ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, App.cAppDisplayName, verbs, string.Empty);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"register file type failed: {ex}");
        }
    }

    private static void UnregisterFileTypeActivation()
    {
        string[] fileTypes = [App.cFileExt];

        try
        {
            ActivationRegistrationManager.UnregisterForFileTypeActivation(fileTypes, Environment.ProcessPath);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"unregister file type failed: {ex}");
        }
    }

    private static void DeleteAppData()
    {
        try
        {
            DirectoryInfo di = new DirectoryInfo(App.GetAppDataPath());
            di.Delete(true);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }
    }
}