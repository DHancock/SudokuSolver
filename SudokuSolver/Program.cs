using Microsoft.UI.Dispatching;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver;

public static class Program
{
    private const string cAppKey = "586A28D4-3FF0-42B2-829B-5F02BBFC8352";

    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [STAThread]
    static async Task Main(string[] args)
    {
        if (Bootstrap.TryInitialize(FindRollForwardSdkVersion(3000), out int hresult))
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

    private static uint FindRollForwardSdkVersion(uint minimumPackageMajorVersion)
    {
        const string cMicrosoft = "8wekyb3d8bbwe";

        object lockObject = new object();
        PackageManager packageManager = new PackageManager();
        ProcessorArchitecture architecture = GetProcessorArchitecture();

        uint latestPackageMajorVersion = minimumPackageMajorVersion;

        Parallel.ForEach(packageManager.FindPackagesForUserWithPackageTypes("", PackageTypes.Main), package =>
        {
            if ((package.Id.Architecture == architecture) &&
                (package.Id.Version.Major > minimumPackageMajorVersion) &&
                string.Equals(package.Id.PublisherId, cMicrosoft, StringComparison.Ordinal) &&
                package.Id.FullName.StartsWith("Microsoft.WinAppRuntime.DDLM.") &&
                (package.Dependencies.Count == 1))
            {
                // check the DDLM package has a dependency on a framework package
                Windows.ApplicationModel.Package dependency = package.Dependencies[0];

                if (dependency.IsFramework &&
                    dependency.Id.FullName.StartsWith("Microsoft.WindowsAppRuntime.1."))
                {
                    lock (lockObject)
                    {
                        if (latestPackageMajorVersion < dependency.Id.Version.Major)
                            latestPackageMajorVersion = dependency.Id.Version.Major;
                    }
                }
            }
        });

        return 0x00010000 + (latestPackageMajorVersion / 1000);
    }

    private static ProcessorArchitecture GetProcessorArchitecture()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86: return ProcessorArchitecture.X86;
            case Architecture.X64: return ProcessorArchitecture.X64;
            case Architecture.Arm64: return ProcessorArchitecture.Arm64;

            default: return ProcessorArchitecture.Unknown;
        }
    }
}
