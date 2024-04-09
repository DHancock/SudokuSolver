using Microsoft.UI.Dispatching;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

public static class Program
{
    private const string cAppKey = "586A28D4-3FF0-42B2-829B-5F02BBFC8352";

    [STAThread]
    static void Main(string[] args)
    {
        bool isRegister = (args.Length == 1) && string.Equals(args[0], "/register", StringComparison.Ordinal);
        bool isUnregister = (args.Length == 1) && string.Equals(args[0], "/unregister", StringComparison.Ordinal);

        if (isRegister)
        {
            RegisterFileTypeActivation();
        }
        else if (isUnregister)
        {
            UnregisterFileTypeActivation();
        }
        else
        {
            AppInstance appInstance = AppInstance.FindOrRegisterForKey(cAppKey);

            if (!appInstance.IsCurrent)
            {
                AppActivationArguments aea = AppInstance.GetCurrent().GetActivatedEventArgs();
                RedirectActivationTo(aea, appInstance);
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
        string[] fileTypes = [App.cFileExt];
        string[] verbs = ["open"];
        string logo = $"{Environment.ProcessPath},0";

        ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, App.Instance.AppDisplayName, verbs, string.Empty);
    }

    private static void UnregisterFileTypeActivation()
    {
        string[] fileTypes = [App.cFileExt];

        ActivationRegistrationManager.UnregisterForFileTypeActivation(fileTypes, Environment.ProcessPath);
    }

    public static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        ManualResetEventSlim mres = new ManualResetEventSlim();

        // avoids the need for an async main entry point, which breaks the clipboard...
        Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            mres.Set();
        });

        mres.Wait();
    }
}