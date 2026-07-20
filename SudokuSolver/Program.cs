using Microsoft.UI.Dispatching;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if ((args.Length == 1) && (args[0] == "/register"))
        {
            RegisterFileTypeActivation();
        }
        else if ((args.Length == 1) && (args[0] == "/unregister"))
        {
            UnregisterFileTypeActivation();
        }
        else
        {
            AppInstance appInstance = AppInstance.FindOrRegisterForKey("586A28D4-3FF0-42B2-829B-5F02BBFC8352");

            if (!appInstance.IsCurrent)
            {
                AppActivationArguments aea = AppInstance.GetCurrent().GetActivatedEventArgs();
                appInstance.RedirectActivationToAsync(aea).AsTask().Wait();
            }
            else
            {
                // Create the installer mutexes with current user access.
                // The app is installed per user rather than all users.
                const string name = "51ECE64E-1954-41C4-81FB-E3A60CE4C224";

                PInvoke.CreateMutex(null, false, name);
                PInvoke.CreateMutex(null, false, "Global\\" + name);

                Application.Start((p) =>
                {
                    DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
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
            Debug.WriteLine(ex.ToString());
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
            Debug.WriteLine(ex.ToString());
        }
    }
}