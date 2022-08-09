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

        Activated += (s, a) =>
        {
            ThemeHelper.Instance.UpdateTheme(PuzzleView.ViewModel.IsDarkThemed);
        };

        if (AppWindowTitleBar.IsCustomizationSupported() && appWindow.TitleBar is not null)
        {
            CustomTitle.Text = cDefaultWindowTitle;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            ThemeHelper.Instance.Register(LayoutRoot, appWindow.TitleBar);
        }
        else
        {
            CustomTitle.Text = cDefaultWindowTitle;
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(CustomTitleBar);
            ThemeHelper.Instance.Register(LayoutRoot);
        }

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

            Title = $"{cDefaultWindowTitle} - {file.Name}";
        }
        catch (Exception ex)
        {
            Title = cDefaultWindowTitle;
            string heading = $"Failed to open file \"{file.Name}\"";
            ShowModalMessage(heading, ex.Message);
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

    private void SaveCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = true;  // TODO: need a dirty flag, can only save if the source was a file
    }

    private void SaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        // TODO:
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
                using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                {
                    using (Stream stream = transaction.Stream.AsStreamForWrite())
                    {
                        PuzzleView.ViewModel?.Save(stream);
                    }

                    await transaction.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                string heading = $"Failed to save file \"{file.Name}\"";
                ShowModalMessage(heading, ex.Message);
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

    private async void SaveSettings()
    {
        try
        {
            string path = GetSettingsFilePath();
            string? directory = Path.GetDirectoryName(path);
            Debug.Assert(!string.IsNullOrWhiteSpace(directory));

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, PuzzleView.ViewModel?.SerializeSettings(), Encoding.Unicode, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    private void AboutClickHandler(object sender, RoutedEventArgs e)
    {
        // TODO:
    }

    public async void ShowModalMessage(string heading, string message)
    {
        ContentDialog messageDialog = new ContentDialog()
        {
            XamlRoot = this.Content.XamlRoot,
            Title = heading,
            Content = message,
            PrimaryButtonText = "OK"
        };

        await messageDialog.ShowAsync();
    }

    public static BitmapImage LoadWindowIconImage() => LoadEmbeddedImageResource("Sudoku.Resources.app.png");

    private static BitmapImage LoadEmbeddedImageResource (string resourcePath)
    {
        BitmapImage bitmapImage = new BitmapImage();

        using (Stream? resourceStream = typeof(App).Assembly.GetManifestResourceStream(resourcePath))
        {
            Debug.Assert(resourceStream is not null);

            using (IRandomAccessStream stream = resourceStream.AsRandomAccessStream())
            {
                bitmapImage.SetSource(stream);
            }
        }

        return bitmapImage;
    }
}
