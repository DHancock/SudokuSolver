
namespace SudokuSolver.Views;

internal class SessionHelper
{
    public bool IsExit { get; set; } = false;   // saving all windows on exit may be required

    private readonly XElement root;

    public SessionHelper()
    {
        root = new XElement("session", new XAttribute("version", 1));
    }

    public void AddWindow(MainWindow mainWindow)
    {
        XElement window = mainWindow.GetSessionData();
        root.Add(window);
    }

    private static string GetSessionPath()
    {
        return Path.Combine(App.GetAppDataPath(), "session.xml");
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(App.GetAppDataPath());

            await using (Stream stream = new FileStream(GetSessionPath(), FileMode.Create))
            {
                await root.SaveAsync(stream, SaveOptions.DisableFormatting, CancellationToken.None);
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
            await using (Stream stream = new FileStream(GetSessionPath(), FileMode.Open, FileAccess.Read))
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
                                    // In this first version this should only happen if the user has been editing the 
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
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }


    private static void CreateWindow(XElement window)
    {
        XElement? data = window.Element("bounds");

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

            // Always set the window state is to Normal, similar to Notepad. It could be confusing if there are multiple
            // windows and one is maximized. At least the windows z order would have to be saved and restored as well,  
            // not impossible but probably best to keep it simple.
            MainWindow parentWindow = new MainWindow(WindowState.Normal, restoreBounds);

            foreach (XElement child in window.Descendants())
            {
                if (child.Name == "puzzle")
                {
                    parentWindow.AddTab(new PuzzleTabViewItem(parentWindow, child));
                }
                else if (child.Name == "settings")
                {
                    parentWindow.AddTab(new SettingsTabViewItem(parentWindow, child));
                }
            }

            parentWindow.Activate();
        }
    }
}
