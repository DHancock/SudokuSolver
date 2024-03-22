using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;


internal sealed partial class PuzzleTabViewItem : TabViewItem, ITabItem
{
    private enum Error { Success, Failure }
    private enum Status { Cancelled, Continue }
    private RelayCommand CloseOtherTabsCommand { get; }
    private RelayCommand CloseLeftTabsCommand { get; }
    private RelayCommand CloseRightTabsCommand { get; }

    private PuzzleViewModel? viewModel;
    private StorageFile? sourceFile;
    private readonly MainWindow parentWindow;

    public PuzzleTabViewItem(MainWindow parent)
    {
        this.InitializeComponent();

        parentWindow = parent;
        ViewModel = new PuzzleViewModel();

        FileMenuItem.Unloaded += (s, a) => FocusLastSelectedCell();
        ViewMenuItem.Unloaded += (s, a) => FocusLastSelectedCell();
        EditMenuItem.Unloaded += (s, a) => FocusLastSelectedCell();

        Loaded += (s, e) =>
        {
            // set for the next theme transition
            Puzzle.BackgroundBrushTransition.Duration = TimeSpan.FromMilliseconds(250);
            FocusLastSelectedCell();
        };

        Clipboard.ContentChanged += async (s, o) =>
        {
            await ViewModel.ClipboardContentChanged();
        };

        CloseOtherTabsCommand = new RelayCommand(ExecuteCloseOtherTabs, CanCloseOtherTabs);
        CloseLeftTabsCommand = new RelayCommand(ExecuteCloseLeftTabs, CanCloseLeftTabs);
        CloseRightTabsCommand = new RelayCommand(ExecuteCloseRightTabs, CanCloseRightTabs);
    }

    public PuzzleTabViewItem(MainWindow parent, StorageFile storageFile) : this(parent)
    {
        sourceFile = storageFile;
        Loaded += LoadedHandler;

        static async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandler;
            tab.Header = tab.sourceFile?.Name;

            if (await tab.LoadFile(tab.sourceFile!) != Error.Success)
            {
                tab.sourceFile = null;
            }

            tab.UpdateTabHeader();
        }
    }

    public PuzzleTabViewItem(MainWindow parent, PuzzleTabViewItem source) : this(parent)
    {
        ViewModel = source.ViewModel;
        sourceFile = source.sourceFile;
        Loaded += LoadedHandler;

        static void LoadedHandler(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandler;
            tab.UpdateTabHeader();
        }
    }

    public PuzzleViewModel ViewModel
    {
        get => viewModel!;
        set
        {
            Debug.Assert(value is not null);

            if (viewModel is not null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            viewModel = value;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            Puzzle.ViewModel = value;
        }
    }

    public StorageFile? SourceFile => sourceFile;

    public void FocusLastSelectedCell()
    {
        if (!parentWindow.IsContentDialogOpen() && IsSelected && parentWindow.IsActive)
        {
            Puzzle.FocusLastSelectedCell();
        }
    }

    public async Task<bool> SaveTabContents()
    {
        Debug.Assert(IsModified);

        return await SaveExistingFirst() != Status.Cancelled;
    }

    public void AdjustKeyboardAccelerators(bool enable)
    {
        // accelerators on sub menus are only active when the menu is shown
        // which can only happen if this is the current selected tab
        foreach (MenuBarItem mbi in Menu.Items)
        {
            foreach (MenuFlyoutItemBase mfib in mbi.Items)
            {
                foreach (KeyboardAccelerator ka in mfib.KeyboardAccelerators)
                {
                    ka.IsEnabled = enable;
                }
            }
        }
    }

    public static bool IsPrintingAvailable => !IntegrityLevel.IsElevated && PrintManager.IsSupported();

    public static bool IsFileDialogAvailable => !IntegrityLevel.IsElevated;

    public bool IsModified => ViewModel!.IsModified;

    private void NewTabClickHandler(object sender, RoutedEventArgs e)
    {
        TabViewItem tab = new PuzzleTabViewItem(parentWindow);
        parentWindow.AddTab(tab);
    }

    private async void OpenClickHandler(object sender, RoutedEventArgs e)
    {
        Status status = Status.Continue;

        if (IsModified)
        {
            status = await SaveExistingFirst();
        }

        if (status != Status.Cancelled)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(openPicker, parentWindow.WindowPtr);
            openPicker.FileTypeFilter.Add(App.cFileExt);

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file is not null)
            {
                Error error = await LoadFile(file);

                if (error == Error.Success)
                {
                    sourceFile = file;
                    UpdateTabHeader();
                    AddToRecentFilesJumpList();
                }
            }
        }
        
        FocusLastSelectedCell();
    }

    private unsafe void AddToRecentFilesJumpList()
    {
        Debug.Assert(sourceFile is not null);

        if (sourceFile is not null)
        {
            const uint SHARD_PathW = 0x03;

            fixed (char* lpStringLocal = sourceFile.Path)
            {
                PInvoke.SHAddToRecentDocs(SHARD_PathW, lpStringLocal);
            }
        }
    }

    private void NewWindowClickHandler(object sender, RoutedEventArgs e)
    {
        MainWindow window = new MainWindow(WindowState.Normal, parentWindow.RestoreBounds);
        window.AddTab(new PuzzleTabViewItem(window));
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
            await parentWindow.PrintPuzzle(this);
        }
        catch (Exception ex)
        {
            await new ErrorDialog("A printing error occurred.", ex.Message, XamlRoot, ActualTheme).ShowAsync();
        }
    }

    private async void CloseTabClickHandler(object sender, RoutedEventArgs e)
    {
        if (IsModified)
        {
            if (await SaveTabContents())
            {
                parentWindow.CloseTab(this);
            }
        }
        else
        {
            parentWindow.CloseTab(this);
        }
    }

    private void CloseWindowClickHandler(object sender, RoutedEventArgs e)
    {
        parentWindow.PostCloseMessage();
    }

    private void ExitClickHandler(object sender, RoutedEventArgs e)
    {
        App.Instance.AttemptCloseAllWindows();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        parentWindow.AddOrSelectSettingsTab();
    }

    private async Task<Error> LoadFile(StorageFile file)
    {
        Error error = Error.Failure;

        try
        {
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                ViewModel.Open(stream);
                error = Error.Success;
            }
        }
        catch (Exception ex)
        {
            string heading = $"An error occurred when opening {file.Name}";
            await new ErrorDialog(heading, ex.Message, XamlRoot, ActualTheme).ShowAsync();
        }

        return error;
    }

    private async Task<Status> SaveExistingFirst()
    {
        Debug.Assert(!parentWindow.IsContentDialogOpen());

        Status status = Status.Continue;
        string path = (sourceFile is null) ? App.cNewPuzzleName : sourceFile.Path;

        ContentDialogResult result = await new ConfirmSaveDialog(path, XamlRoot, LayoutRoot.ActualTheme).ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            status = await Save();  // if it's a new file, the Save As picker could be canceled
        }
        else if (result == ContentDialogResult.None)
        {
            status = Status.Cancelled;
        }

        return status;
    }

    private async Task SaveFile(StorageFile file)
    {
        using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
        {
            using (Stream stream = transaction.Stream.AsStreamForWrite())
            {
                ViewModel.Save(stream);

                // delete any existing file data beyond the end of the stream
                transaction.Stream.Size = transaction.Stream.Position;

                await transaction.CommitAsync();
            }
        }
    }

    private async Task<Status> Save()
    {
        Status status = Status.Cancelled;

        if (sourceFile is not null)
        {
            try
            {
                await SaveFile(sourceFile);
                status = Status.Continue;
            }
            catch (Exception ex)
            {
                string heading = $"An error occurred when saving {sourceFile.Name}";
                await new ErrorDialog(heading, ex.Message, XamlRoot, ActualTheme).ShowAsync();
            }
        }
        else
        {
            status = await SaveAs();
        }

        return status;
    }

    private async Task<Status> SaveAs()
    {
        Status status = Status.Cancelled;

        FileSavePicker savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, parentWindow.WindowPtr);
        savePicker.FileTypeChoices.Add("Sudoku files", new List<string>() { App.cFileExt });
        savePicker.SuggestedFileName = (sourceFile is null) ? App.cNewPuzzleName : sourceFile.Name;

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file is not null)
        {
            try
            {
                await SaveFile(file);
                sourceFile = file;
                UpdateTabHeader();
                AddToRecentFilesJumpList();
                status = Status.Continue;
            }
            catch (Exception ex)
            {
                string heading = $"An error occurred when saving {file.Name}";
                await new ErrorDialog(heading, ex.Message, XamlRoot, ActualTheme).ShowAsync();
            }
        }

        return status;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsModified))
        {
            UpdateTabHeader();
        }
    }

    public void UpdateTabHeader()
    {
        if (sourceFile is null)
        {
            Header = App.cNewPuzzleName;
            ToolTipService.SetToolTip(this, App.cNewPuzzleName);
        }
        else
        {
            Header = sourceFile.Name;
            ToolTipService.SetToolTip(this, sourceFile.Path);
        }

        if (IsModified && IconSource is null)
        {
            IconSource = new SymbolIconSource() { Symbol = Symbol.Edit, };
        }
        else if (!IsModified && IconSource is not null)
        {
            IconSource = null;
        }
    }

    public void ResetOpacityTransitionForThemeChange()
    {
        Puzzle.ResetOpacityTransitionForThemeChange();
    }

    private bool CanCloseOtherTabs(object? param)
    {
        return parentWindow.CanCloseOtherTabs();
    }

    private async void ExecuteCloseOtherTabs(object? param)
    {
        await parentWindow.ExecuteCloseOtherTabs();
    }

    private bool CanCloseLeftTabs(object? param)
    {
        return parentWindow.CanCloseLeftTabs();
    }

    private async void ExecuteCloseLeftTabs(object? param)
    {
        await parentWindow.ExecuteCloseLeftTabs();
    }

    private bool CanCloseRightTabs(object? param)
    {
        return parentWindow.CanCloseRightTabs();
    }

    private async void ExecuteCloseRightTabs(object? param)
    {
        await parentWindow.ExecuteCloseRightTabs();
    }
}