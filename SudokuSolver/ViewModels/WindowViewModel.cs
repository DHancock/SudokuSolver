namespace Sudoku.ViewModels;

internal sealed class WindowViewModel : INotifyPropertyChanged
{
    public Settings.PerViewSettings ViewSettings { get; }

    private string title = string.Empty;


    public WindowViewModel(Settings.PerViewSettings viewSettings)
    {
        ViewSettings = viewSettings;
    }

    public string Title
    {
        get => title;
        set
        {
            if (string.CompareOrdinal(title, value) != 0)
            {
                title = value;
                NotifyPropertyChanged();
            }
        }
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
