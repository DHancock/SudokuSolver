using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Input;

namespace Sudoku.Views
{
    internal static class ErrorDialog
    {
        public static async void Show(MetroWindow parent, string heading, string details)
        {
            MetroDialogSettings dialogSettings = new MetroDialogSettings()
            {
                // A bug in MahApps stops access keys from working in this dialog. The
                // Esc and Enter keys both work though so not really too much of a problem...
                AffirmativeButtonText = "Close",
                ColorScheme = MetroDialogColorScheme.Accented,
            };

            // Another bug in MahApps is that the dialog isn't as modal as it should be.
            // The parent window's command bindings will still be active while the dialog
            // is open i.e. typing Control+P opens the print dialog.  
            CommandBindingCollection backup = new CommandBindingCollection(parent.CommandBindings);
            parent.CommandBindings.Clear();

            await parent.ShowMessageAsync(heading, details, MessageDialogStyle.Affirmative, dialogSettings);

            parent.CommandBindings.AddRange(backup);
        }
    }
}
