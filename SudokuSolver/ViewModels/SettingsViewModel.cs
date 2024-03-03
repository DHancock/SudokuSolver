using SudokuSolver.Utilities;

namespace SudokuSolver.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    private static readonly string[] cValueKeys = ["UserCellBrush", "ProvidedCellBrush", "CalculatedCellBrush", "CellPossiblesBrush", "PossiblesHorizontalBrush", "PossiblesVerticalBrush"];
    private static readonly string[] cPropertyNames = ["User", "Provided", "Calculated", "Possible", "HPossible", "VPossible"];

    public RelayCommand ResetLightColors { get; }
    public RelayCommand ResetDarkColors { get; }

    // This singlton class mirrors the Settings singleton. The Settings singleton is responcible for reading and writing
    // the data. Whereas this class handles notification changes for ui binding, fowarding any data to the Settings class
    public static SettingsViewModel Data = new SettingsViewModel();

    private SettingsViewModel()
    {
        ResetLightColors = new RelayCommand(ExecuteResetLightColors, CanExecuteResetLightColors);
        ResetDarkColors = new RelayCommand(ExecuteResetDarkColors, CanExecuteResetDarkColors);
    }

    public ElementTheme Theme
    {
        get => Settings.Data.Theme;
        set
        {
            if (value != Settings.Data.Theme)
            {
                Settings.Data.Theme = value;

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsLightTheme));
                NotifyPropertyChanged(nameof(IsDarkTheme));
                NotifyPropertyChanged(nameof(IsSystemTheme));
            }
        }
    }

    public static bool IsLightTheme => Settings.Data.Theme == ElementTheme.Light;
    public static bool IsDarkTheme => Settings.Data.Theme == ElementTheme.Dark;
    public static bool IsSystemTheme => Settings.Data.Theme == ElementTheme.Default;
    
    public bool ShowPossibles
    {
        get => Settings.Data.ViewSettings.ShowPossibles;
        set
        {
            Settings.Data.ViewSettings.ShowPossibles = value;
            NotifyPropertyChanged();
        }
    }

    public bool ShowSolution
    {
        get => Settings.Data.ViewSettings.ShowSolution;
        set
        {
            Settings.Data.ViewSettings.ShowSolution = value;
            NotifyPropertyChanged();
        }
    }

    public Color UserLight { get => GetterLight(0); set => SetterLight(0, value); }
    public Color ProvidedLight { get => GetterLight(1); set => SetterLight(1, value); }
    public Color CalculatedLight { get => GetterLight(2); set => SetterLight(2, value); }
    public Color PossibleLight { get => GetterLight(3); set => SetterLight(3, value); }
    public Color HPossibleLight { get => GetterLight(4); set => SetterLight(4, value); }
    public Color VPossibleLight { get => GetterLight(5); set => SetterLight(5, value); }

    public Color UserDark { get => GetterDark(0); set => SetterDark(0, value); }
    public Color ProvidedDark { get => GetterDark(1); set => SetterDark(1, value); }
    public Color CalculatedDark { get => GetterDark(2); set => SetterDark(2, value); }
    public Color PossibleDark { get => GetterDark(3); set => SetterDark(3, value); }
    public Color HPossibleDark { get => GetterDark(4); set => SetterDark(4, value); }
    public Color VPossibleDark { get => GetterDark(5); set => SetterDark(5, value); }

    private static Color GetterLight(int index) => Settings.Data.LightThemeColors[index];
    private static Color GetterDark(int index) => Settings.Data.DarkThemeColors[index];

    private void SetterLight(int index, Color value, [CallerMemberName] string? propertyName = default)
    {
        Setter(isLight: true, index, value, Settings.Data.LightThemeColors, propertyName);
    }

    private void SetterDark(int index, Color value, [CallerMemberName] string? propertyName = default)
    {
        Setter(isLight: false, index, value, Settings.Data.DarkThemeColors, propertyName);
    }

    private void Setter(bool isLight, int index, Color value, List<Color> colors, string? propertyName)
    {
        if (value != colors[index])
        {
            colors[index] = value;
            NotifyPropertyChanged(propertyName);

            if (isLight)
            {
                UpdateResourceThemeColors("Light", colors);
                ResetLightColors.RaiseCanExecuteChanged();
            }
            else
            {
                UpdateResourceThemeColors("Dark", colors);
                ResetDarkColors.RaiseCanExecuteChanged();
            }
        }
    }

    private void ExecuteResetLightColors(object? param)
    {
        for (int index = 0; index < cPropertyNames.Length; index++)
        {
            SetterLight(index, Settings.Data.DefaultLightThemeColors[index], $"{cPropertyNames[index]}Light");
        }
    }

    private void ExecuteResetDarkColors(object? param)
    {
        for (int index = 0; index < cPropertyNames.Length; index++)
        {
            SetterDark(index, Settings.Data.DefaultDarkThemeColors[index], $"{cPropertyNames[index]}Dark");
        }
    }

    private bool CanExecuteResetLightColors(object? param)
    {
        return !Enumerable.SequenceEqual(Settings.Data.DefaultLightThemeColors, Settings.Data.LightThemeColors);
    }

    private bool CanExecuteResetDarkColors(object? param)
    {
        return !Enumerable.SequenceEqual(Settings.Data.DefaultDarkThemeColors, Settings.Data.DarkThemeColors);
    }

    public static List<Color> ReadResourceThemeColors(string themeKey)
    {
        ResourceDictionary? theme = Utils.GetThemeDictionary(themeKey);

        Debug.Assert(theme is not null);
        Debug.Assert(Array.TrueForAll(cValueKeys, x => theme.ContainsKey(x)));
        Debug.Assert(Array.TrueForAll(cValueKeys, x => theme[x] is SolidColorBrush));

        List<Color> colors = new List<Color>(cValueKeys.Length);

        foreach(string key in cValueKeys)
        {
            colors.Add(((SolidColorBrush)theme[key]).Color);
        }

        return colors;
    }

    public static void UpdateResourceThemeColors(string themeKey, List<Color> colors)
    {
        ResourceDictionary? theme = Utils.GetThemeDictionary(themeKey);

        Debug.Assert(theme is not null);
        Debug.Assert(Array.TrueForAll(cValueKeys, x => theme.ContainsKey(x)));
        Debug.Assert(Array.TrueForAll(cValueKeys, x => theme[x] is SolidColorBrush));

        for (int index = 0; index < colors.Count; index++)
        {
            SolidColorBrush scb = (SolidColorBrush)theme[cValueKeys[index]];

            if (scb.Color != colors[index])
                scb.Color = colors[index];
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
