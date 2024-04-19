using Microsoft.UI.Dispatching;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

public static class Program
{
    private const string cAppKey = "586A28D4-3FF0-42B2-829B-5F02BBFC8352";

    [STAThread]
    static void Main()
    {
        AppInstance appInstance = AppInstance.FindOrRegisterForKey(cAppKey);

        if (!appInstance.IsCurrent)
        {
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            appInstance.RedirectActivationToAsync(args).AsTask().Wait();
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