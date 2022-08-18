using Microsoft.UI.Xaml;

using Sudoku.ViewModels;

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

        puzzleView.ViewModel = new PuzzleViewModel(ReadSettings());

        appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));

        appWindow.Closing += (s, a) =>
        {
            puzzleView.ViewModel.WindowPlacement = GetWindowPlacement();
            SaveSettings();
        };

        customTitleBar.AppWindow = appWindow;
        customTitleBar.Title = cDefaultWindowTitle;

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
        else
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(customTitleBar);
        }
        

        printHelper = new PrintHelper(hWnd, Content.XamlRoot);

        SetWindowPlacement(puzzleView.ViewModel.WindowPlacement);

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
            ViewModel = puzzleView.ViewModel,
        };

        printHelper.PrintView(printView, clientArea.RequestedTheme);
    }

    public bool IsPrintingAvailable => printHelper.IsPrintingAvailable;

    private async void OpenFile(StorageFile file)
    {
        try
        {
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                puzzleView.ViewModel?.Open(stream);
            }

            SourceFile = file;
            customTitleBar.Title = $"{cDefaultWindowTitle} - {file.DisplayName}";
        }
        catch (Exception ex)
        {
            customTitleBar.Title = cDefaultWindowTitle;
            string heading = $"Failed to open file \"{file.DisplayName}\"";
            await new ErrorDialog(heading, ex.Message, Content.XamlRoot, clientArea.RequestedTheme).ShowAsync();
        }
    }

    private async void OpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        FileOpenPicker openPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.FileTypeFilter.Add(cDefaultFileExt);
        
        StorageFile file = await openPicker.PickSingleFileAsync();

        if (file is not null)
            OpenFile(file);
    }

    private async void SaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        if (SourceFile is not null)
        {
            try
            {
                await SaveFile(SourceFile);
            }
            catch (Exception ex)
            {
                string heading = $"Failed to save the puzzle as \"{SourceFile.DisplayName}\"";
                await new ErrorDialog(heading, ex.Message, Content.XamlRoot, clientArea.RequestedTheme).ShowAsync();

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

        if (file is not null)
        {
            try
            {
                await SaveFile(file);
                SourceFile = file;
                customTitleBar.Title = $"{cDefaultWindowTitle} - {file.DisplayName}";
            }
            catch (Exception ex)
            {
                string heading = $"Failed to save the puzzle as \"{file.DisplayName}\"";
                await new ErrorDialog(heading, ex.Message, Content.XamlRoot, clientArea.RequestedTheme).ShowAsync();
            }
        }
    }

    private async Task SaveFile (StorageFile file)
    {
        using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
        {
            using (Stream stream = transaction.Stream.AsStreamForWrite())
            {
                puzzleView.ViewModel?.Save(stream);

                // delete any existing file data beyond the end of the stream
                transaction.Stream.Size = transaction.Stream.Position;

                await transaction.CommitAsync();
            }
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

            File.WriteAllText(path, puzzleView.ViewModel?.SerializeSettings(), Encoding.Unicode);
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

    private async void AboutClickHandler(object sender, RoutedEventArgs e)
    {
        await new AboutBox(Content.XamlRoot, clientArea.RequestedTheme).ShowAsync();
    }

    public static async Task<BitmapImage> LoadEmbeddedImageResource(string resourcePath)
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
