namespace SudokuSolver.Views;

internal class SessionHelper
{
    private readonly XElement root;

    public SessionHelper()
    {
        root = new XElement("session", new XAttribute("version", 1));
    }

    public void AddWindow(MainWindow mainWindow)
    {
        XElement window = mainWindow.GetSessionData();

        if (window.HasElements)
        {
            root.Add(window);
        }
    }

    private static string GetSessionPath()
    {
        return Path.Combine(App.GetAppDataPath(), "session.xml");
    }

    public void Save()
    {
        try
        {
            if (root.HasElements)
            {
                Directory.CreateDirectory(App.GetAppDataPath());

                using (Stream stream = File.Create(GetSessionPath()))
                {
                    root.Save(stream, SaveOptions.DisableFormatting);
                }
            }
            else
            {
                File.Delete(GetSessionPath());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    public static async Task LoadPreviousSessionAsync()
    {
        try
        {
            await using (Stream stream = File.OpenRead(GetSessionPath()))
            {
                XDocument document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

                if (document.Root is not null)
                {
                    if ((document.Root.Name == "session") && (document.Root.Attribute("version") is XAttribute va) && int.TryParse(va.Value, out int version))
                    {
                        if (version == 1)
                        {
                            IEnumerable<XElement> windows = document.Descendants("window");

                            foreach (XElement window in windows)
                            {
                                if (!MainWindow.ValidateSessionData(window))
                                {
                                    // This should only happen if the user has been editing the 
                                    // session file, in which case all bets are off...
                                    return;
                                }
                            }

                            foreach (XElement window in windows)
                            {
                                CreateWindow(window);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private static void CreateWindow(XElement root)
    {
        XElement? data = root.Element("bounds");

        if (data is not null)
        {
            RectInt32 restoreBounds = default;

            foreach (XElement child in data.Descendants())
            {
                switch (child.Name.ToString())
                {
                    case nameof(RectInt32.X): restoreBounds.X = Convert.ToInt32(child.Value); break;
                    case nameof(RectInt32.Y): restoreBounds.Y = Convert.ToInt32(child.Value); break;
                    case nameof(RectInt32.Width): restoreBounds.Width = Convert.ToInt32(child.Value); break;
                    case nameof(RectInt32.Height): restoreBounds.Height = Convert.ToInt32(child.Value); break;
                }
            }

            if ((root.Element("puzzle") is not null) || (root.Element("settings") is not null))
            {
                // Always open with window state Normal, as does Notepad.
                // It could be confusing if there are multiple windows and one is maximized.
                MainWindow window = new MainWindow(WindowState.Normal, restoreBounds);
                
                foreach (XElement child in root.Descendants())
                {
                    if (child.Name == "puzzle")
                    {
                        window.AddTab(new PuzzleTabViewItem(window, child));
                    }
                    else if (child.Name == "settings")
                    {
                        window.AddTab(new SettingsTabViewItem(window, child));
                    }
                }
            }
        }
    }
}
