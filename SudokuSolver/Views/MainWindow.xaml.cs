using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Sudoku.Utilities;
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

    public MainWindow(StorageFile? storagefile)
    {
        InitializeComponent();

        // each window needs a local copy of the common view settings
        Settings.PerViewSettings viewSettings = Settings.Data.ViewSettings.Clone();

        // acrylic also works, but isn't recommended according to the UI guidelines
        if (!TrySetMicaBackdrop(viewSettings.Theme))
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

        appWindow.Closing += async (s, args) =>
        {
            // the await call creates a continuation routine, so must cancel the close here
            // and handle the actual closing explicitly when the continuation routine runs...
            args.Cancel = true;
            await HandleWindowClosing();
        };

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            CustomTitleBar.ParentAppWindow = appWindow;
            CustomTitleBar.UpdateThemeAndTransparency(viewSettings.Theme);
            Activated += CustomTitleBar.ParentWindow_Activated;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            // the last event received will have the correct dimensions
            layoutRoot.SizeChanged += (s, a) => SetWindowDragRegions();
            Menu.SizeChanged += (s, a) => SetWindowDragRegions();
            Puzzle.SizeChanged += (s, a) => SetWindowDragRegions();

            layoutRoot.Loaded += (s, a) => SetWindowDragRegions();
            Menu.Loaded += (s, a) => SetWindowDragRegions();
            Puzzle.Loaded += (s, a) => SetWindowDragRegions();
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

        if (storagefile is not null)
        {
            layoutRoot.Loaded += async (s, e) =>
            {
                Error error = await OpenFile(storagefile);

                if (error == Error.Success)
                    SourceFile = storagefile;
            };
        }
    }

    private async Task HandleWindowClosing()
    {
        Status status = await SaveExistingFirst();

        if (status != Status.Cancelled)
        {
            bool lastWindow = App.UnRegisterWindow(this);

            if (lastWindow)
            {
                Settings.Data.RestoreBounds = RestoreBounds;
                Settings.Data.WindowState = WindowState;
                await Settings.Data.Save();
            }

            // calling Close() doesn't raise an AppWindow.Closing event
            Close();
        }
    }

    private RectInt32 ValidateRestoreBounds(RectInt32 windowArea)
    {
        if (windowArea == default)
            return CenterInPrimaryDisplay();

        RectInt32 workArea = DisplayArea.GetFromRect(windowArea, DisplayAreaFallback.Nearest).WorkArea;
        PointInt32 position = new PointInt32(windowArea.X, windowArea.Y);

        if ((position.Y + windowArea.Height) > workArea.Bottom())
            position.Y = workArea.Bottom() - windowArea.Height;

        if (position.Y < workArea.Y)
            position.Y = workArea.Y;

        if ((position.X + windowArea.Width) > workArea.Right())
            position.X = workArea.Right() - windowArea.Width;

        if (position.X < workArea.X)
            position.X = workArea.X;

        SizeInt32 size = new SizeInt32(Math.Min(windowArea.Width, workArea.Width),
                                        Math.Min(windowArea.Height, workArea.Height));

        return new RectInt32(position.X, position.Y, size.Width, size.Height);
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

    private void NewWindowClickHandler(object sender, RoutedEventArgs e)
    {
        App.CreateNewWindow(null);
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
        await HandleWindowClosing();
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
        string filePart = SourceFile is null ? App.cNewPuzzleName : SourceFile.DisplayName;
        string modified = Puzzle.ViewModel!.PuzzleModified ? "*" : string.Empty;

        string title;

        if (layoutRoot.FlowDirection == FlowDirection.LeftToRight)
            title = $"{App.cDisplayName} - {filePart}{modified}";
        else
            title = $"{modified}{filePart} - {App.cDisplayName}";

        if (AppWindowTitleBar.IsCustomizationSupported())
            CustomTitleBar.Title = title;
        else
            Title = title;
    }

    private void SetWindowDragRegions()
    {
        if (layoutRoot.IsLoaded)
        {
            Debug.Assert(AppWindowTitleBar.IsCustomizationSupported());
            Debug.Assert(appWindow.TitleBar.ExtendsContentIntoTitleBar);

            double scale = layoutRoot.XamlRoot.RasterizationScale;

            // make any part of the window that isn't a control, a drag area (the mica parts)
            RectInt32 windowRect = new RectInt32(0, 0, appWindow.ClientSize.Width, appWindow.ClientSize.Height);
            RectInt32 menuRect = ScaledRect(Menu.ActualOffset.X, Menu.ActualOffset.Y, Menu.ActualWidth, Menu.ActualHeight, scale);

#if ISSUE_FIXED 
            RectInt32 puzzleRect = ScaledRect(Puzzle.ActualOffset.X, Puzzle.ActualOffset.Y, Puzzle.ActualWidth, Puzzle.ActualHeight, scale);
#else
            // see https://github.com/microsoft/microsoft-ui-xaml/issues/7756
            // Can't add the area to the left of the puzzle because the menu drop downs will
            // intersect with the drag rectangles, and then won't respond to mouse...
            RectInt32 puzzleRect = ScaledRect(0, Menu.ActualOffset.Y + Menu.ActualHeight, Puzzle.ActualOffset.X + Puzzle.ActualWidth, (Puzzle.ActualOffset.Y + Puzzle.ActualHeight) - (Menu.ActualOffset.Y + Menu.ActualHeight), scale);
#endif

            Utils.SimpleRegion region = new Utils.SimpleRegion(windowRect);
            region.Subtract(menuRect);
            region.Subtract(puzzleRect);

            appWindow.TitleBar.SetDragRectangles(region.ToArray());

#if false 
            // <Canvas x:Name="DebugCanvas" Opacity="0.5" Grid.RowSpan="3"/>

            DebugCanvas.Children.Clear();

            SolidColorBrush[] brushes = new SolidColorBrush[4];
            brushes[0] = new SolidColorBrush(Colors.Green);
            brushes[1] = new SolidColorBrush(Colors.Red);
            brushes[2] = new SolidColorBrush(Colors.Yellow);
            brushes[3] = new SolidColorBrush(Colors.Blue);

            RectInt32[] rects = region.ToArray();

            for (int index = 0; index < rects.Length; index++)
            {
                RectInt32 rect = rects[index];

                Rectangle rectangle = new Rectangle();
                rectangle.Fill = brushes[index % 4];
                rectangle.Width = rect.Width / scale;
                rectangle.Height = rect.Height / scale;
                Canvas.SetLeft(rectangle, rect.X / scale);
                Canvas.SetTop(rectangle, rect.Y / scale);

                DebugCanvas.Children.Add(rectangle);
            }
#endif
        }
    }

    RectInt32 ScaledRect(double x, double y, double width, double height, double scale)
    {
        return new RectInt32(Convert.ToInt32(x * scale), 
                                Convert.ToInt32(y * scale), 
                                Convert.ToInt32(width * scale), 
                                Convert.ToInt32(height * scale));
    }
}
