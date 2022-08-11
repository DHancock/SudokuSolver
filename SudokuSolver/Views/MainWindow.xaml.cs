using Sudoku.ViewModels;
using Sudoku.Utils;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : SubClassWindow
{
    // according to https://fileinfo.com this extension isn't in use (at least by a popular program)
    private const string cDefaultFilterName = "Sudoku files";
    private const string cDefaultFileExt = ".sdku";
    private const string cDefaultWindowTitle = "Sudoku Solver";

    private readonly AppWindow appWindow;
    private readonly PrintHelper printHelper;
    private StorageFile? SourceFile { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        PuzzleView.ViewModel = new PuzzleViewModel(ReadSettings());

        appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));

        appWindow.Closing += (s, a) =>
        {
            PuzzleView.ViewModel.WindowPlacement = GetWindowPlacement();
            SaveSettings();
        };

        WindowTitle.Text = cDefaultWindowTitle;

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            ThemeHelper.Instance.Register(LayoutRoot, appWindow.TitleBar);
        }
        else
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(CustomTitleBar);
            ThemeHelper.Instance.Register(ClientArea);
        }
        
        ThemeHelper.Instance.UpdateTheme(PuzzleView.ViewModel.IsDarkThemed);

        LoadWindowIconImage();

        printHelper = new PrintHelper(hWnd, Content.XamlRoot);

        SetWindowPlacement(PuzzleView.ViewModel.WindowPlacement);

        ProcessCommandLine(Environment.GetCommandLineArgs());
    }

    private async void ProcessCommandLine(string[] args)
    {
        // args[0] is typically the full path of the executing assembly
        if ((args?.Length == 2) && (Path.GetExtension(args[1]).ToLower() == cDefaultFileExt) && File.Exists(args[1]))
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(args[1]);
            OpenFile(file);
        }
    }

    private void ExitClickHandler(object sender, RoutedEventArgs e) => Close();

    private void PrintClickHandler(object sender, RoutedEventArgs e)
    {
        PuzzleView printView = new PuzzleView
        {
            RequestedTheme = ElementTheme.Light,
            ViewModel = PuzzleView.ViewModel,
        };

        printHelper.PrintView(printView);
    }

    public static bool IsPrintingSupported => PrintHelper.IsSupported;

    private async void OpenFile(StorageFile file)
    {
        try
        {
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                PuzzleView.ViewModel?.Open(stream);
            }

            SourceFile = file;
            WindowTitle.Text = $"{cDefaultWindowTitle} - {file.DisplayName}";
        }
        catch (Exception ex)
        {
            WindowTitle.Text = cDefaultWindowTitle;
            string heading = $"Failed to open file \"{file.DisplayName}\"";
            ShowModalMessage(heading, ex.Message, Content.XamlRoot);
        }
    }

    private async void OpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        FileOpenPicker openPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.FileTypeFilter.Add(cDefaultFileExt);
        
        StorageFile file = await openPicker.PickSingleFileAsync();

        if (file != null)
            OpenFile(file);
    }

    private async void SaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        if (SourceFile != null)
        {
            try
            {
                await SaveFile(SourceFile);
            }
            catch (Exception ex)
            {
                string heading = $"Failed to save the puzzle as \"{SourceFile.DisplayName}\"";
                ShowModalMessage(heading, ex.Message, Content.XamlRoot);
            }
        }  
        else
            SaveAsClickHandler(sender, new RoutedEventArgs());
    }

    private async void SaveAsClickHandler(object sender, RoutedEventArgs e)
    {
        FileSavePicker savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, hWnd);

        savePicker.FileTypeChoices.Add(cDefaultFilterName, new List<string>() { cDefaultFileExt });
        savePicker.SuggestedFileName = "New Puzzle";

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file != null)
        {
            try
            {
                await SaveFile(file);
                SourceFile = file;
                WindowTitle.Text = $"{cDefaultWindowTitle} - {file.DisplayName}";
            }
            catch (Exception ex)
            {
                string heading = $"Failed to save the puzzle as \"{file.DisplayName}\"";
                ShowModalMessage(heading, ex.Message, Content.XamlRoot);
            }
        }
    }

    private async Task SaveFile (StorageFile file)
    {
        using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
        {
            using (Stream stream = transaction.Stream.AsStreamForWrite())
            {
                PuzzleView.ViewModel?.Save(stream);
            }

            await transaction.CommitAsync();
        }
    }


    private static string ReadSettings()
    {
        string path = GetSettingsFilePath();

        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        return string.Empty;
    }

    private void SaveSettings()
    {
        try
        {
            string path = GetSettingsFilePath();
            string? directory = Path.GetDirectoryName(path);
            Debug.Assert(!string.IsNullOrWhiteSpace(directory));

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, PuzzleView.ViewModel?.SerializeSettings(), Encoding.Unicode);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    private static string GetSettingsFilePath()
    {
        const string cFileName = "settings.json";
        const string cDirName = "SudokuSolver.6D40B575-644E-43C8-9856-D74A50EA1352";

        return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), cDirName, cFileName);
    }

    private void AboutClickHandler(object sender, RoutedEventArgs e)
    {
        // TODO:
    }

    public async static void ShowModalMessage(string heading, string message, XamlRoot xamlRoot)
    {
        ContentDialog messageDialog = new ContentDialog()
        {
            XamlRoot = xamlRoot,
            Title = heading,
            Content = message,
            PrimaryButtonText = "OK"
        };

        await messageDialog.ShowAsync();
    }

    private async void LoadWindowIconImage()
    {
        WindowIcon.Source = await LoadEmbeddedImageResource("Sudoku.Resources.app.png");
    }

    private static async Task<BitmapImage> LoadEmbeddedImageResource(string resourcePath)
    {
        BitmapImage bitmapImage = new BitmapImage();

        using (Stream? resourceStream = typeof(App).Assembly.GetManifestResourceStream(resourcePath))
        {
            Debug.Assert(resourceStream is not null);

            using (IRandomAccessStream stream = resourceStream.AsRandomAccessStream())
            {
                await bitmapImage.SetSourceAsync(stream);
            }
        }

        return bitmapImage;
    }
}
