using Sudoku.ViewModels;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : SubClassWindow
{
    private readonly PrintHelper printHelper;
    private StorageFile? SourceFile { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        if (!TrySetMicaBackdrop())  // acrylic also works, but isn't recommended according to the UI guidelines
        {
            layoutRoot.Loaded += (s, e) =>
            {
                // the visual states won't exist until after OnApplyTemplate() has completed
                bool stateFound = VisualStateManager.GoToState(layoutRoot, "BackdropNotSupported", false);
                Debug.Assert(stateFound);
            };
        }

        puzzleView.ViewModel = new PuzzleViewModel();

        appWindow.Closing += async (s, a) =>
        {
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.WindowState = WindowState;
            await Settings.Data.Save();
        };

        WindowTitle = App.cDisplayName;

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            customTitleBar.ParentAppWindow = appWindow;
            Activated += customTitleBar.ParentWindow_Activated;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
        else
        {
            customTitleBar.Visibility = Visibility.Collapsed;
        }

        // always set the window icon, it's used in the task switcher
        SetWindowIconFromAppIcon();

        printHelper = new PrintHelper(this, DispatcherQueue);

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

        layoutRoot.Loaded += async (s, e) =>
        {
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();

            if (args.Kind == ExtendedActivationKind.File)
            {
                if ((args.Data is IFileActivatedEventArgs fileData) && (fileData.Files.Count > 0))
                {
                    // if multiple files are selected, a separate app instance will be started for each file
                    await ProcessCommandLine(new[] { string.Empty, fileData.Files[0].Path });
                }
            }
            else if (args.Kind == ExtendedActivationKind.Launch)
            {
                await ProcessCommandLine(Environment.GetCommandLineArgs());
            }
        };
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

    private async Task ProcessCommandLine(string[] args)
    {
        // args[0] is typically the full path of the executing assembly
        if ((args?.Length == 2) && (Path.GetExtension(args[1]).ToLower() == App.cFileExt) && File.Exists(args[1]))
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(args[1]);
            await OpenFile(file);
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

            await printHelper.PrintViewAsync(PrintCanvas, printView);
        }
        catch (Exception ex)
        {
            await new ErrorDialog("A printing error occured", ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
        }
    }

#pragma warning disable CA1822 // Mark members as static
    public bool IsPrintingAvailable => PrintManager.IsSupported();
#pragma warning restore CA1822 // Mark members as static

    private async Task OpenFile(StorageFile file)
    {
        try
        {
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                puzzleView.ViewModel?.Open(stream);
            }

            SourceFile = file;
            WindowTitle = $"{App.cDisplayName} - {file.DisplayName}";
        }
        catch (Exception ex)
        {
            WindowTitle = App.cDisplayName;
            string heading = $"Failed to open file \"{file.DisplayName}\"";
            await new ErrorDialog(heading, ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
        }
    }

    private async void OpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        FileOpenPicker openPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.FileTypeFilter.Add(App.cFileExt);
        
        StorageFile file = await openPicker.PickSingleFileAsync();

        if (file is not null)
            await OpenFile(file);
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
                await new ErrorDialog(heading, ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
            }
        }  
        else
            SaveAsClickHandler(sender, new RoutedEventArgs());
    }

    private async void SaveAsClickHandler(object sender, RoutedEventArgs e)
    {
        FileSavePicker savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, hWnd);

        savePicker.FileTypeChoices.Add("Sudoku files", new List<string>() { App.cFileExt });
        savePicker.SuggestedFileName = "New Puzzle";

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file is not null)
        {
            try
            {
                await SaveFile(file);
                SourceFile = file;
                WindowTitle = $"{App.cDisplayName} - {file.DisplayName}";
            }
            catch (Exception ex)
            {
                string heading = $"Failed to save the puzzle as \"{file.DisplayName}\"";
                await new ErrorDialog(heading, ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
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
        await new AboutBox(Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
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
