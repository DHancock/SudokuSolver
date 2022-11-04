using Sudoku.Views;

namespace Sudoku.ViewModels;

// These settings are serialized to a json text file.
// Adding or deleting properties is safe. The missing, or extra data is ignored.
// Changing the type of an existing property may cause problems though. Best not
// delete properties just in case a name is later reused with a different type.

internal class Settings
{
    public bool ShowPossibles { get; set; } = false;

    public bool ShowSolution { get; set; } = true;

    public bool IsDarkThemed { get; set; } = Application.Current.RequestedTheme == ApplicationTheme.Dark;

    public WindowState WindowState { get; set; } = WindowState.Normal;

    public Rect RestoreBounds { get; set; } = Rect.Empty;
}

