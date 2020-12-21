using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Sudoku.Views
{
    internal static class ErrorDialog
    {
        public static async void Show(MetroWindow parent, string heading, string details)
        {
            MetroDialogSettings dialogSettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Close",
                ColorScheme = MetroDialogColorScheme.Accented,
            };

            await parent.ShowMessageAsync(heading, details, MessageDialogStyle.Affirmative, dialogSettings);
        }
    }
}
