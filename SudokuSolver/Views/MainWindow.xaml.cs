using Sudoku.ViewModels;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : SubClassWindow
{
    private enum Error { Success, Failure }
    private enum Status { Cancelled, Continue }
    public WindowViewModel ViewModel { get; private set; }

    private readonly PrintHelper printHelper;

    private StorageFile? sourceFile;

    public MainWindow(bool openedFromActivation = false)
    {
        InitializeComponent();

        // each window needs a local copy of the common view settings
        Settings.PerViewSettings viewSettings = Settings.Data.ViewSettings.Clone();

        // acrylic also works, but isn't recommended according to the UI guidelines
        if (!TrySetMicaBackdrop(viewSettings.IsDarkThemed ? ElementTheme.Dark : ElementTheme.Light))
        {
            layoutRoot.Loaded += (s, e) =>
            {
                // the visual states won't exist until after OnApplyTemplate() has completed
                bool stateFound = VisualStateManager.GoToState(layoutRoot, "BackdropNotSupported", false);
                Debug.Assert(stateFound);
            };
        }

        ViewModel = new WindowViewModel(viewSettings);
        Puzzle.ViewModel = new PuzzleViewModel(viewSettings);
        Puzzle.ViewModel.PropertyChanged += ViewModel_PropertyChanged;  // used to update the window title

        appWindow.Closing += async (s, a) =>
        {
            bool lastWindow = App.UnRegisterWindow(this);

            if (lastWindow)
            {
                Settings.Data.RestoreBounds = RestoreBounds;
                Settings.Data.WindowState = WindowState;
                await Settings.Data.Save();
            }
        };

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            CustomTitleBar.ParentAppWindow = appWindow;
            Activated += CustomTitleBar.ParentWindow_Activated;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
        else
        {
            CustomTitleBar.Visibility = Visibility.Collapsed;
        }

        // always set the window icon, it's used in the task switcher
        SetWindowIconFromAppIcon();
        UpdateWindowTitle();

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

        if (openedFromActivation)
        {
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
            Error error = await OpenFile(file);

            if (error == Error.Success)
                SourceFile = file;
        }
    }

    private async void NewClickHandler(object sender, RoutedEventArgs e)
    {
        Status status = await SaveExistingFirst();

        if (status != Status.Cancelled)
        {
            Puzzle.ViewModel!.New();
            SourceFile = null;
        }
    }

    private async void OpenClickHandler(object sender, RoutedEventArgs e)
    {
        Status status = await SaveExistingFirst();

        if (status != Status.Cancelled)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.FileTypeFilter.Add(App.cFileExt);

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file is not null)
            {
                Error error = await OpenFile(file);

                if (error == Error.Success)
                    SourceFile = file;
            }
        }
    }

    private async void NewWindowClickHandler(object sender, RoutedEventArgs e)
    {
        if (!App.CreateNewWindow(openedFromActivation: false))
        {
            string heading = "A new window couldn't be opened.";
            string details = "The maximum number of open windows has been reached.";
            await new ErrorDialog(heading, details, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
        }
    }

    private async void SaveClickHandler(object sender, RoutedEventArgs e)
    {
        await Save();
    }

    private async void SaveAsClickHandler(object sender, RoutedEventArgs e)
    {
        await SaveAs();
    }

    private async void PrintClickHandler(object sender, RoutedEventArgs e)
    {
        try
        {
            PuzzleView printView = new PuzzleView
            {
                RequestedTheme = ElementTheme.Light,
                ViewModel = Puzzle.ViewModel,
            };

            await printHelper.PrintViewAsync(PrintCanvas, printView);
        }
        catch (Exception ex)
        {
            await new ErrorDialog("A printing error occurred.", ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
        }
    }

    private async void CloseClickHandler(object sender, RoutedEventArgs e)
    {
        Status status = await SaveExistingFirst();

        if (status != Status.Cancelled)
            Close();
    }

#pragma warning disable CA1822 // Mark members as static
    public bool IsPrintingAvailable => PrintManager.IsSupported();
#pragma warning restore CA1822 // Mark members as static

    private async Task<Error> OpenFile(StorageFile file)
    {
        Error error = Error.Failure;

        try
        {
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                Puzzle.ViewModel!.Open(stream);
                error = Error.Success;
            }
        }
        catch (Exception ex)
        {
            string heading = $"An error occurred when opening {file.DisplayName}.";
            await new ErrorDialog(heading, ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
        }

        return error;
    }

    private async Task<Status> SaveExistingFirst()
    {
        Status status = Status.Continue;

        if (Puzzle.ViewModel!.PuzzleModified)
        {
            string path;

            if (SourceFile is null)
                path = App.cNewPuzzleName;
            else
                path = SourceFile.Path;

            ContentDialogResult result = await new ConfirmSaveDialog(path, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                status = await Save();  // if it's a new file, the Save As picker could be cancelled
            }
            else if (result == ContentDialogResult.None)
            {
                status = Status.Cancelled;
            }
        }

        return status;
    }

    private async Task SaveFile(StorageFile file)
    {
        using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
        {
            using (Stream stream = transaction.Stream.AsStreamForWrite())
            {
                Puzzle.ViewModel?.Save(stream);

                // delete any existing file data beyond the end of the stream
                transaction.Stream.Size = transaction.Stream.Position;

                await transaction.CommitAsync();
            }
        }
    }

    private async Task<Status> Save()
    {
        Status status = Status.Continue;

        if (SourceFile is not null)
        {
            try
            {
                await SaveFile(SourceFile);
            }
            catch (Exception ex)
            {
                string heading = $"An error occurred when saving {SourceFile.DisplayName}.";
                await new ErrorDialog(heading, ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
            }
        }
        else
            status = await SaveAs();

        return status;
    }

    private async Task<Status> SaveAs()
    {
        Status status = Status.Cancelled;
        FileSavePicker savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, hWnd);

        savePicker.FileTypeChoices.Add("Sudoku files", new List<string>() { App.cFileExt });

        if (SourceFile is null)
            savePicker.SuggestedFileName = App.cNewPuzzleName;
        else
            savePicker.SuggestedFileName = SourceFile.DisplayName;

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file is not null)
        {
            try
            {
                await SaveFile(file);
                SourceFile = file;
                status = Status.Continue;
            }
            catch (Exception ex)
            {
                string heading = $"An error occurred when saving {file.DisplayName}.";
                await new ErrorDialog(heading, ex.Message, Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
            }
        }

        return status; 
    }

    private async void AboutClickHandler(object sender, RoutedEventArgs e)
    {
        await new AboutBox(Content.XamlRoot, layoutRoot.ActualTheme).ShowAsync();
    }

    private StorageFile? SourceFile
    {
        get => sourceFile;
        set
        {
            if (sourceFile != value)
            {
                sourceFile = value;
                UpdateWindowTitle();
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Puzzle.ViewModel.PuzzleModified))
            UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        bool isModified = Puzzle.ViewModel!.PuzzleModified;
        string filePart;

        if (SourceFile is not null)
            filePart = $"{SourceFile.DisplayName}{(isModified ? "*" : string.Empty)}";
        else
            filePart = $"{App.cNewPuzzleName}{(isModified ? "*" : string.Empty)}";

        string title;

        if (layoutRoot.FlowDirection == FlowDirection.LeftToRight)
            title = $"{App.cDisplayName} - {filePart}";
        else
            title = $"{filePart} - {App.cDisplayName}";

        if (AppWindowTitleBar.IsCustomizationSupported())
            CustomTitleBar.Title = title;
        else
            Title = title;
    }
}
