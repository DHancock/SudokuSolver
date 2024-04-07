using SudokuSolver.Views;

namespace SudokuSolver.ViewModels;

internal class Settings
{
    public static Settings Data = Inner.Load();

    public PerViewSettings ViewSettings { get; set; } = new PerViewSettings();
    public PerPrintSettings PrintSettings { get; set; } = new PerPrintSettings();
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public RectInt32 RestoreBounds { get; set; } = default;
    public ElementTheme Theme { get; set; } = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;

    public List<Color> LightThemeColors { get; set; }
    public List<Color> DarkThemeColors { get; set; }

    [JsonIgnore]
    public List<Color> DefaultLightThemeColors { get; set; }

    [JsonIgnore]
    public List<Color> DefaultDarkThemeColors { get; set; }

    public bool SaveSessionState { get; set; } = true;

    private Settings()
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
        await Inner.SaveAsync(this);
    }

    private sealed class Inner : Settings
    {
        // Json deserialization requires a public parameterless constructor.
        // That breaks the singleton pattern, so use a private inner inherited class
        public Inner()
        {
        }

        public static async Task SaveAsync(Settings settings)
        {
            try
            {
                Directory.CreateDirectory(App.GetAppDataPath());

                await File.WriteAllTextAsync(GetSettingsFilePath(), JsonSerializer.Serialize(settings, GetSerializerOptions()));
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        public static Settings Load()
        {
            string path = GetSettingsFilePath();

            if (File.Exists(path))
            {
                try
                {
                    string data = File.ReadAllText(path);

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        Settings? settings = JsonSerializer.Deserialize<Inner>(data, GetSerializerOptions());

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
                    Debug.Fail(ex.Message);
                }
            }

            return new Settings();
        }

        private static string GetSettingsFilePath()
        {
            return Path.Join(App.GetAppDataPath(), "settings.json");
        }

        private static JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                WriteIndented = true,
                IncludeFields = true,
            };
        }
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

