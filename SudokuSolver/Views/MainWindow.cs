// copied from: (edited for nullable, and application specific theme, rather than following the system default)
// https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/SamplePages/SampleSystemBackdropsWindow.xaml.cs

namespace SudokuSolver.Views
{
    internal sealed partial class MainWindow : Window
    {
        WindowsSystemDispatcherQueueHelper? m_wsdqHelper; // See separate sample below for implementation
        Microsoft.UI.Composition.SystemBackdrops.MicaController? m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController? m_acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration? m_configurationSource;

#pragma warning disable IDE0051 // Remove unused private members
        bool TrySetAcrylicBackdrop(ElementTheme initialTheme)
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme(initialTheme);

                m_acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Acrylic is not supported on this system
        }

        bool TrySetMicaBackdrop(ElementTheme initialTheme)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme(initialTheme);

                m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (m_configurationSource != null)
                m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }

            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }

            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            SetConfigurationSourceTheme(sender.ActualTheme);
        }

        private void SetConfigurationSourceTheme(ElementTheme theme)
        {
            if (m_configurationSource != null)
            {
                switch (theme)
                {
                    case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
                }
            }
        }
    }

    class WindowsSystemDispatcherQueueHelper
    {
        DispatcherQueueController? m_dispatcherQueueController;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = (uint)Marshal.SizeOf<DispatcherQueueOptions>();
                options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
                options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA;

                HRESULT hResult = PInvoke.CreateDispatcherQueueController(options, out m_dispatcherQueueController);
                Debug.Assert(hResult.Succeeded);
            }
        }
    }
}

// copied here because of CsWin32 issue:
// https://github.com/microsoft/CsWin32/issues/397

// sourced from:
// https://github.com/microsoft/CsWin32/blob/main/src/Microsoft.Windows.CsWin32/templates/WinRTCustomMarshaler.cs

namespace Windows.Win32.CsWin32.InteropServices
{
    internal class WinRTCustomMarshaler : global::System.Runtime.InteropServices.ICustomMarshaler
    {
        private readonly string winrtClassName;
        private bool lookedForFromAbi;
        private global::System.Reflection.MethodInfo? fromAbi;

        private WinRTCustomMarshaler(string cookie)
        {
            this.winrtClassName = cookie;
        }

        /// <summary>
        /// Gets an instance of the marshaler given a cookie
        /// </summary>
        /// <param name="cookie">Cookie used to create marshaler</param>
        /// <returns>Marshaler</returns>
        public static global::System.Runtime.InteropServices.ICustomMarshaler GetInstance(string cookie)
        {
            return new WinRTCustomMarshaler(cookie);
        }

        void global::System.Runtime.InteropServices.ICustomMarshaler.CleanUpManagedData(object ManagedObj)
        {
        }

        void global::System.Runtime.InteropServices.ICustomMarshaler.CleanUpNativeData(global::System.IntPtr pNativeData)
        {
            global::System.Runtime.InteropServices.Marshal.Release(pNativeData);
        }

        int global::System.Runtime.InteropServices.ICustomMarshaler.GetNativeDataSize()
        {
            throw new global::System.NotImplementedException();
        }

        global::System.IntPtr global::System.Runtime.InteropServices.ICustomMarshaler.MarshalManagedToNative(object ManagedObj)
        {
            throw new global::System.NotImplementedException();
        }

        object global::System.Runtime.InteropServices.ICustomMarshaler.MarshalNativeToManaged(global::System.IntPtr pNativeData)
        {
            if (!this.lookedForFromAbi)
            {
                var assembly = typeof(global::Windows.Foundation.IMemoryBuffer).Assembly;
                var type = global::System.Type.GetType($"{this.winrtClassName}, {assembly.FullName}");

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                this.fromAbi = type.GetMethod("FromAbi");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                this.lookedForFromAbi = true;
            }

            if (this.fromAbi != null)
            {
#pragma warning disable CS8603 // Possible null reference return.
                return this.fromAbi.Invoke(null, new object[] { pNativeData });
#pragma warning restore CS8603 // Possible null reference return.
            }
            else
            {
                return global::System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(pNativeData);
            }
        }
    }
}