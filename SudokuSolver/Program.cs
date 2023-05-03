using Microsoft.UI.Dispatching;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

public static class Program
{
    private const string cAppKey = "586A28D4-3FF0-42B2-829B-5F02BBFC8352";

    [STAThread]
    static async Task Main(string[] args)
    {
        if (Bootstrap.TryInitialize(0x00010003, null, new PackageVersion(3000, 820, 152), Bootstrap.InitializeOptions.OnNoMatch_ShowUI, out int hresult))
        {
            try
            {
                if ((args.Length == 1) && string.Equals(args[0], "/register", StringComparison.Ordinal))
                {
                    RegisterFileTypeActivation();
                }
                else if ((args.Length == 1) && string.Equals(args[0], "/unregister", StringComparison.Ordinal))
                {
                    UnregisterFileTypeActivation();
                }
                else
                {
                    AppInstance appInstance = AppInstance.FindOrRegisterForKey(cAppKey);

                    if (!appInstance.IsCurrent)
                    {
                        AppActivationArguments aea = AppInstance.GetCurrent().GetActivatedEventArgs();
                        await appInstance.RedirectActivationToAsync(aea);
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
            finally
            {
                Bootstrap.Shutdown();
            }
        }
    }

    private static void RegisterFileTypeActivation()
    {
        string[] fileTypes = new[] { App.cFileExt };
        string[] verbs = new[] { "open" };
        string logo = $"{Environment.ProcessPath},0";

        ActivationRegistrationManager.RegisterForFileTypeActivation(fileTypes, logo, App.cDisplayName, verbs, string.Empty);
    }

    private static void UnregisterFileTypeActivation()
    {
        string[] fileTypes = new[] { App.cFileExt };

        ActivationRegistrationManager.UnregisterForFileTypeActivation(fileTypes, Environment.ProcessPath);
    }
}
