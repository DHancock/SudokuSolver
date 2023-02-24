using SudokuSolver.Views;

namespace SudokuSolver.ViewModels;

// Windows.Storage.ApplicationData isn't supported in unpackaged apps.

// For the unpackaged variant, settings are serialized to a json text file.
// Adding or deleting properties is safe. The missing, or extra data is ignored.
// Changing the type of an existing property may cause problems though. Best not
// delete properties just in case a name is later reused with a different type.

internal class Settings
{
    public static Settings Data = Inner.Load();

    private Settings()
    {
    }

    public PerViewSettings ViewSettings { get; set; } = new PerViewSettings();

    public PerPrintSettings PrintSettings { get; set; } = new PerPrintSettings();

    public WindowState WindowState { get; set; } = WindowState.Normal;

    public RectInt32 RestoreBounds { get; set; } = default;

    public async Task Save()
    {
        await Inner.Save(this);
    }

    private sealed class Inner : Settings
    {
        // Json deserialization requires a public parameterless constructor.
        // That breaks the singleton pattern, so use a private inner inherited class
        public Inner()
        {
        }

        public static async Task Save(Settings settings)
        {
            if (App.IsPackaged)
                SavePackaged(settings);
            else
                await SaveUnpackaged(settings);
        }

        private static void SavePackaged(Settings settings)
        {
            try
            {
                IPropertySet properties = ApplicationData.Current.LocalSettings.Values;

                properties[nameof(PerViewSettings.IsDarkThemed)] = settings.ViewSettings.IsDarkThemed;
                properties[nameof(PerViewSettings.ShowSolution)] = settings.ViewSettings.ShowSolution;
                properties[nameof(PerViewSettings.ShowPossibles)] = settings.ViewSettings.ShowPossibles;

                properties[nameof(PerPrintSettings.PrintAlignment)] = (int)settings.PrintSettings.PrintAlignment;
                properties[nameof(PerPrintSettings.PrintSize)] = (int)settings.PrintSettings.PrintSize;
                properties[nameof(PerPrintSettings.PrintMargin)] = (int)settings.PrintSettings.PrintMargin;
                properties[nameof(PerPrintSettings.ShowHeader)] = settings.PrintSettings.ShowHeader;

                properties[nameof(WindowState)] = (int)settings.WindowState;
                properties[nameof(RectInt32.X)] = settings.RestoreBounds.X;
                properties[nameof(RectInt32.Y)] = settings.RestoreBounds.Y;
                properties[nameof(RectInt32.Width)] = settings.RestoreBounds.Width;
                properties[nameof(RectInt32.Height)] = settings.RestoreBounds.Height;
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        private static async Task SaveUnpackaged(Settings settings)
        {
            try
            {
                string path = GetSettingsFilePath();
                string? directory = Path.GetDirectoryName(path);
                Debug.Assert(!string.IsNullOrWhiteSpace(directory));

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, GetSerializerOptions()));
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        public static Settings Load()
        {
            if (App.IsPackaged)
                return LoadPackaged();

            return LoadUnpackaged();
        }

        private static Settings LoadPackaged()
        {
            Settings settings = new Settings();

            try
            {
                IPropertySet properties = ApplicationData.Current.LocalSettings.Values;

                settings.ViewSettings.IsDarkThemed = (bool)properties[nameof(PerViewSettings.IsDarkThemed)];
                settings.ViewSettings.ShowSolution = (bool)properties[nameof(PerViewSettings.ShowSolution)];
                settings.ViewSettings.ShowPossibles = (bool)properties[nameof(PerViewSettings.ShowPossibles)];

                settings.PrintSettings.PrintAlignment = (PrintHelper.Alignment)properties[nameof(PerPrintSettings.PrintAlignment)];
                settings.PrintSettings.PrintSize = (PrintHelper.PrintSize)properties[nameof(PerPrintSettings.PrintSize)];
                settings.PrintSettings.PrintMargin = (PrintHelper.Margin)properties[nameof(PerPrintSettings.PrintMargin)];
                settings.PrintSettings.ShowHeader = (bool)properties[nameof(PerPrintSettings.ShowHeader)];

                settings.WindowState = (WindowState)properties[nameof(WindowState)];

                settings.RestoreBounds = new RectInt32((int)properties[nameof(RectInt32.X)],
                                                    (int)properties[nameof(RectInt32.Y)],
                                                    (int)properties[nameof(RectInt32.Width)],
                                                    (int)properties[nameof(RectInt32.Height)]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return settings;
        }

        private static Settings LoadUnpackaged()
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
                            return settings;
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
            const string cFileName = "settings.json";
            const string cDirName = "SudokuSolver.davidhancock.net";
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Path.Join(localAppData, cDirName, cFileName);
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
        public bool IsDarkThemed { get; set; } = Application.Current.RequestedTheme == ApplicationTheme.Dark;
        public bool ShowPossibles { get; set; } = false;
        public bool ShowSolution { get; set; } = true;

        [JsonIgnore]
        public ElementTheme Theme  => IsDarkThemed ? ElementTheme.Dark : ElementTheme.Light;

        public PerViewSettings Clone() => (PerViewSettings)this.MemberwiseClone();
    }

    internal sealed class PerPrintSettings
    {
        public PrintHelper.PrintSize PrintSize { get; set; } = PrintHelper.PrintSize.Size_80;
        public PrintHelper.Alignment PrintAlignment { get; set; } = PrintHelper.Alignment.MiddleCenter;
        public PrintHelper.Margin PrintMargin { get; set; } = PrintHelper.Margin.None;
        public bool ShowHeader { get; set; } = false;

        public PerPrintSettings Clone() => (PerPrintSettings)this.MemberwiseClone();
    }
}

  