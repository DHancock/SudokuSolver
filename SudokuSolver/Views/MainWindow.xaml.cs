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

    
    private readonly PrintHelper printHelper;
    private StorageFile? SourceFile { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        puzzleView.ViewModel = new PuzzleViewModel();

        appWindow.Closing += async (s, a) =>
        {
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.WindowState = WindowState;
            await Settings.Data.Save();
        };

        WindowTitle = cDefaultWindowTitle;

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            customTitleBar.ParentWindow = appWindow;
            Activated += customTitleBar.ParentWindow_Activated;
            DpiChanged += customTitleBar.ParentWindow_DpiChanged;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        } 
        else
        {
            customTitleBar.Visibility = Visibility.Collapsed;
            SetWindowIconFromAppIcon();
        }

        printHelper = new PrintHelper(hWnd, DispatcherQueue);

        if (Settings.Data.IsFirstRun)
        {
            appWindow.MoveAndResize(CenterInPrimaryDisplay());
        }
        else
        {
            appWindow.MoveAndResize(ValidateRestoreBounds(Settings.Data.RestoreBounds));

            if (Settings.Data.WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            else
                WindowState = Settings.Data.WindowState;
        }

        ProcessCommandLine(Environment.GetCommandLineArgs());
    }

    private RectInt32 ValidateRestoreBounds(RectInt32 windowArea)
    {
        if (windowArea == default)
            return CenterInPrimaryDisplay();

        RectInt32 workArea = DisplayArea.GetFromRect(windowArea, DisplayAreaFallback.Nearest).WorkArea;
        PointInt32 position = new PointInt32(windowArea.X, windowArea.Y);

        if ((position.Y + windowArea.Height) > (workArea.Y + workArea.Height))
            position.Y = (workArea.Y + workArea.Height) - windowArea.Height;

        if (position.Y < workArea.Y)
            position.Y = workArea.Y;

        if ((position.X + windowArea.Width) > (workArea.X + workArea.Width))
            position.X = (workArea.X + workArea.Width) - windowArea.Width;

        if (position.X < workArea.X)
            position.X = workArea.X;

        SizeInt32 size = new SizeInt32(Math.Min(windowArea.Width, workArea.Width), 
                                        Math.Min(windowArea.Height, workArea.Height));

        return new RectInt32(position.X, position.Y, size.Width, size.Height);
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

    private async void PrintClickHandler(object sender, RoutedEventArgs e)
    {
        try
        {
            PuzzleView printView = new PuzzleView
            {
                RequestedTheme = ElementTheme.Light,
                ViewModel = puzzleView.ViewModel,
            };

            await printHelper.PrintViewAsync(printView);
        }
        catch (Exception ex)
        {
            await new ErrorDialog("A printing error occured", ex.Message, Content.XamlRoot, clientArea.RequestedTheme).ShowAsync();
        }
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
            WindowTitle = $"{cDefaultWindowTitle} - {file.DisplayName}";
        }
        catch (Exception ex)
        {
            WindowTitle = cDefaultWindowTitle;
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
                WindowTitle = $"{cDefaultWindowTitle} - {file.DisplayName}";
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

    private string WindowTitle
    {
        set
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
                customTitleBar.Title = value;
            else
                Title = value;
        }
    }
}
