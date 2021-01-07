using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

#nullable enable

namespace Sudoku.Views
{
    internal static class ErrorDialog
    {
        public static void Show(MetroWindow parent, string heading, string details)
        {
            parent.ShowModalMessageExternal(heading, details);
        }
    }
}                                                               
