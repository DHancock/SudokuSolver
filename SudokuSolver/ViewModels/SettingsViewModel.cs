﻿using SudokuSolver.Utilities;

namespace SudokuSolver.ViewModels;

internal class SettingsViewModel : INotifyPropertyChanged
{
    private static readonly string[] cValueKeys = ["UserCellBrush", "ProvidedCellBrush", "CalculatedCellBrush", "CellPossiblesBrush", "PossiblesHorizontalBrush", "PossiblesVerticalBrush"];
    private static readonly string[] cPropertyNames = ["User", "Provided", "Calculated", "Possible", "HPossible", "VPossible"];

    public RelayCommand ResetLightColors { get; }
    public RelayCommand ResetDarkColors { get; }

    public RelayCommand ResetLightUser { get; }
    public RelayCommand ResetLightProvided { get; }
    public RelayCommand ResetLightCalculated { get; }
    public RelayCommand ResetLightPossible { get; }
    public RelayCommand ResetLightHPossible { get; }
    public RelayCommand ResetLightVPossible { get; }

    public RelayCommand ResetDarkUser { get; }
    public RelayCommand ResetDarkProvided { get; }
    public RelayCommand ResetDarkCalculated { get; }
    public RelayCommand ResetDarkPossible { get; }
    public RelayCommand ResetDarkHPossible { get; }
    public RelayCommand ResetDarkVPossible { get; }


    // This singleton class mirrors the Settings singleton. The Settings singleton is responsible for reading and writing
    // the data. This class handles notification changes for ui binding, forwarding any data to the Settings class
    public static SettingsViewModel Data = new SettingsViewModel();

    private SettingsViewModel()
    {
        ResetLightColors = new RelayCommand(ExecuteResetLightColors, CanExecuteResetLightColors);
        ResetDarkColors = new RelayCommand(ExecuteResetDarkColors, CanExecuteResetDarkColors);

        ResetLightUser = new RelayCommand(p => ResetLightColor(0), p => IsLightColorDifferent(0));
        ResetLightProvided = new RelayCommand(p => ResetLightColor(1), p => IsLightColorDifferent(1));
        ResetLightCalculated = new RelayCommand(p => ResetLightColor(2), p => IsLightColorDifferent(2));
        ResetLightPossible = new RelayCommand(p => ResetLightColor(3), p => IsLightColorDifferent(3));
        ResetLightHPossible = new RelayCommand(p => ResetLightColor(4), p => IsLightColorDifferent(4));
        ResetLightVPossible = new RelayCommand(p => ResetLightColor(5), p => IsLightColorDifferent(5));

        ResetDarkUser = new RelayCommand(p => ResetDarkColor(0), p => IsDarkColorDifferent(0));
        ResetDarkProvided = new RelayCommand(p => ResetDarkColor(1), p => IsDarkColorDifferent(1));
        ResetDarkCalculated = new RelayCommand(p => ResetDarkColor(2), p => IsDarkColorDifferent(2));
        ResetDarkPossible = new RelayCommand(p => ResetDarkColor(3), p => IsDarkColorDifferent(3));
        ResetDarkHPossible = new RelayCommand(p => ResetDarkColor(4), p => IsDarkColorDifferent(4));
        ResetDarkVPossible = new RelayCommand(p => ResetDarkColor(5), p => IsDarkColorDifferent(5));
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

    public bool IsLightTheme
    {
        get => Settings.Data.Theme == ElementTheme.Light;
        set
        {
            if (value)
            {
                Theme = ElementTheme.Light;
            }
        }
    }

    public bool IsDarkTheme
    {
        get => Settings.Data.Theme == ElementTheme.Dark;
        set
        {
            if (value)
            {
                Theme = ElementTheme.Dark;
            }
        }
    }

    public bool IsSystemTheme
    {
        get => Settings.Data.Theme == ElementTheme.Default;
        set
        {
            if (value)
            {
                Theme = ElementTheme.Default;
            }
        }

    }

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

    private void ResetLightColor(int index)
    {
        SetterLight(index, Settings.Data.DefaultLightThemeColors[index], $"{cPropertyNames[index]}Light");
    }

    private static bool IsLightColorDifferent(int index)
    {
        return !Settings.Data.LightThemeColors[index].Equals(Settings.Data.DefaultLightThemeColors[index]);
    }

    private void ResetDarkColor(int index)
    {
        SetterDark(index, Settings.Data.DefaultDarkThemeColors[index], $"{cPropertyNames[index]}Dark");
    }

    private static bool IsDarkColorDifferent(int index)
    {
        return !Settings.Data.DarkThemeColors[index].Equals(Settings.Data.DefaultDarkThemeColors[index]);
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
            {
                scb.Color = colors[index];
            }
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}