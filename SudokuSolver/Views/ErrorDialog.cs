using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using System.Windows;
using System;

#nullable enable

namespace Sudoku.Views
{
    internal static class ErrorDialog
    {
#if UseMahAppsDialog

        // The MahApps in window dialog is visually very good but unfortunately it has problems
        // with modality. Command bindings of the parent window will still be active and focus 
        // can be changed to the parent window using access keys. See:
        // https://github.com/MahApps/MahApps.Metro/issues/2400

        public static async void Show(MetroWindow parent, string heading, string details)
        {
            MetroDialogSettings dialogSettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Close",
            };

            await parent.ShowMessageAsync(heading, details, MessageDialogStyle.Affirmative, dialogSettings);
        }
#else
        public static void Show(MetroWindow _, string heading, string message)
        {
            string text = heading + System.Environment.NewLine + message;
            MessageBox.Show(messageBoxText: text, caption: "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
#endif
    }
}
