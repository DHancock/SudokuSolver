namespace Sudoku.ViewModels;

internal sealed class WindowViewModel : INotifyPropertyChanged
{
    public Settings.PerViewSettings ViewSettings { get; }

    public WindowViewModel(Settings.PerViewSettings viewSettings)
    {
        ViewSettings = viewSettings;
    }

    public ElementTheme Theme
    {
        get => ViewSettings.IsDarkThemed ? ElementTheme.Dark : ElementTheme.Light;
    }

    public bool IsDarkThemed
    {
        get => ViewSettings.IsDarkThemed;
        set
        {
            if (ViewSettings.IsDarkThemed != value)
            {
                ViewSettings.IsDarkThemed = value;
                Settings.Data.ViewSettings.IsDarkThemed = value;
                NotifyPropertyChanged(nameof(Theme));
                NotifyPropertyChanged();
            }
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
