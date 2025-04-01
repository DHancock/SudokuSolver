using SudokuSolver.Views;

namespace SudokuSolver.ViewModels;

internal class Settings
{
    public static Settings Instance = Load();

    public PerViewSettings ViewSettings { get; set; } = new PerViewSettings();
    public PerPrintSettings PrintSettings { get; set; } = new PerPrintSettings();
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public RectInt32 RestoreBounds { get; set; } = default;
    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    public List<Color> LightThemeColors { get; set; }
    public List<Color> DarkThemeColors { get; set; }

    [JsonIgnore]
    public List<Color> DefaultLightThemeColors { get; set; }

    [JsonIgnore]
    public List<Color> DefaultDarkThemeColors { get; set; }

    public bool SaveSessionState { get; set; } = true;
                           
    public bool OneTimeSaveOnEndSession { get; set; } = false;

    // while this breaks the singleton pattern, the code generator doesn't 
    // work with private nested classes. Worse things have happened at sea...
    public Settings()
    {
        // load defaults before the current settings over write them
        DefaultLightThemeColors = SettingsViewModel.ReadResourceThemeColors("Light");
        DefaultDarkThemeColors = SettingsViewModel.ReadResourceThemeColors("Dark");
        // initialize now in case they aren't in the json file
        LightThemeColors = new List<Color>(DefaultLightThemeColors);
        DarkThemeColors = new List<Color>(DefaultDarkThemeColors);
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(App.GetAppDataPath());

            string jsonString = JsonSerializer.Serialize(this, SettingsJsonContext.Default.Settings);
            await File.WriteAllTextAsync(GetSettingsFilePath(), jsonString);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    private static Settings Load()
    {
        string path = GetSettingsFilePath();

        try
        {
            string data = File.ReadAllText(path);

            if (!string.IsNullOrWhiteSpace(data))
            {
                Settings? settings = JsonSerializer.Deserialize<Settings>(data, SettingsJsonContext.Default.Settings);

                if (settings is not null)
                {
                    SettingsViewModel.UpdateResourceThemeColors("Light", settings.LightThemeColors);

                    // if reading an old settings file with fewer custom colors, make up the numbers with default values
                    for (int index = settings.LightThemeColors.Count; index < settings.DefaultLightThemeColors.Count; index++)
                    {
                        settings.LightThemeColors.Add(settings.DefaultLightThemeColors[index]);
                    }

                    SettingsViewModel.UpdateResourceThemeColors("Dark", settings.DarkThemeColors);

                    for (int index = settings.DarkThemeColors.Count; index < settings.DefaultDarkThemeColors.Count; index++)
                    {
                        settings.DarkThemeColors.Add(settings.DefaultLightThemeColors[index]);
                    }

                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        return new Settings();
    }

    private static string GetSettingsFilePath()
    {
        return Path.Join(App.GetAppDataPath(), "settings.json");
    }

    // View specific settings
    // The clone function is used to give each view model it's own copy
    internal sealed class PerViewSettings
    {
        public bool ShowPossibles { get; set; } = false;
        public bool ShowSolution { get; set; } = true;

        public PerViewSettings Clone() => (PerViewSettings)MemberwiseClone();
    }

    internal sealed class PerPrintSettings
    {
        public PrintHelper.PrintSize PrintSize { get; set; } = PrintHelper.PrintSize.Size_80;
        public PrintHelper.Alignment PrintAlignment { get; set; } = PrintHelper.Alignment.MiddleCenter;
        public PrintHelper.Margin PrintMargin { get; set; } = PrintHelper.Margin.None;
        public bool ShowHeader { get; set; } = false;

        public PerPrintSettings Clone() => (PerPrintSettings)MemberwiseClone();
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}
