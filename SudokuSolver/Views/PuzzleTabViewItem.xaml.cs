using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;


internal sealed partial class PuzzleTabViewItem : TabViewItem, ITabItem, ISession
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

            Button? closeButton = this.FindChild<Button>("CloseButton");
            Debug.Assert(closeButton is not null);

            if (closeButton is not null)
            {
                ToolTipService.SetToolTip(closeButton, "Close tab (Ctrl + W)");
            }
        };

        Clipboard.ContentChanged += async (s, o) =>
        {
            await ViewModel.ClipboardContentChangedAsync();
        };

        CloseOtherTabsCommand = new RelayCommand(ExecuteCloseOtherTabsAsync, CanCloseOtherTabs);
        CloseLeftTabsCommand = new RelayCommand(ExecuteCloseLeftTabsAsync, CanCloseLeftTabs);
        CloseRightTabsCommand = new RelayCommand(ExecuteCloseRightTabsAsync, CanCloseRightTabs);
    }

    public PuzzleTabViewItem(MainWindow parent, StorageFile storageFile) : this(parent)
    {
        sourceFile = storageFile;
        Loaded += LoadedHandlerAsync;

        static async void LoadedHandlerAsync(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandlerAsync;
            tab.Header = tab.sourceFile?.Name;

            if (await tab.LoadFileAsync(tab.sourceFile!) != Error.Success)
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

    public PuzzleTabViewItem(MainWindow parent, XElement root) : this(parent)
    {
        Loaded += LoadedHandlerAsync;

        async void LoadedHandlerAsync(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandlerAsync;

            XElement? data = root.Element("path");

            if ((data is not null) && !string.IsNullOrEmpty(data.Value))
            {
                try
                {
                    tab.sourceFile = await StorageFile.GetFileFromPathAsync(data.Value);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            data = root.Element("title");

            if (data is not null)
            {
                tab.Header = data.Value;
            }

            data = root.Element("modified");
            bool isModified = false;

            if (data is not null)
            {
                isModified = data.Value == "true";
            }

            data = root.Element("Sudoku");

            if (data is not null)
            {
                tab.ViewModel.LoadXml(data, isModified);
            }

            tab.UpdateTabHeader();
        }
    }

    public PuzzleViewModel ViewModel
    {
        get => viewModel!;
        set
        {
            if (viewModel is not null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                Puzzle.SelectedIndexChanged -= ViewModel.Puzzle_SelectedIndexChanged;
            }

            viewModel = value;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            Puzzle.ViewModel = value;
        }
    }

    public StorageFile? SourceFile => sourceFile;

    public void FocusLastSelectedCell()
    {
        if (IsLoaded && IsSelected && parentWindow.IsActive && !parentWindow.IsContentDialogOpen())
        {
            Puzzle.FocusLastSelectedCell();
        }
    }

    public async Task<bool> SaveTabContentsAsync()
    {
        Debug.Assert(IsModified);
        return await SaveExistingFirstAsync() != Status.Cancelled;
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

    private async void OpenClickHandlerAsync(object sender, RoutedEventArgs e)
    {
        Status status = Status.Continue;

        if (IsModified)
        {
            status = await SaveExistingFirstAsync();
        }

        if (status != Status.Cancelled)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(openPicker, parentWindow.WindowPtr);
            openPicker.FileTypeFilter.Add(App.cFileExt);

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file is not null)
            {
                Error error = await LoadFileAsync(file);

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

    private async void SaveClickHandlerAsync(object sender, RoutedEventArgs e)
    {
        await SaveAsync();
    }

    private async void SaveAsClickHandlerAsync(object sender, RoutedEventArgs e)
    {
        await SaveAsAsync();
    }

    private async void PrintClickHandlerAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            await parentWindow.PrintPuzzleAsync(this);
        }
        catch (Exception ex)
        {
            await new ErrorDialog("A printing error occurred.", ex.Message, XamlRoot, ActualTheme).ShowAsync();
        }
    }

    private async void CloseTabClickHandlerAsync(object sender, RoutedEventArgs e)
    {
        if (IsModified)
        {
            if (await SaveTabContentsAsync())
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

    private async Task<Error> LoadFileAsync(StorageFile file)
    {
        Error error = Error.Failure;

        try
        {
            await using (Stream stream = await file.OpenStreamForReadAsync())
            {
                XDocument document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

                ViewModel.LoadXml(document.Root, isModified: false);
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

    private async Task<Status> SaveExistingFirstAsync()
    {
        Debug.Assert(!parentWindow.IsContentDialogOpen());

        Status status = Status.Continue;
        string path = (sourceFile is null) ? (string)Header : sourceFile.Path;

        ContentDialogResult result = await new ConfirmSaveDialog(path, XamlRoot, LayoutRoot.ActualTheme).ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            status = await SaveAsync();  // if it's a new file, the Save As picker could be canceled
        }
        else if (result == ContentDialogResult.None)
        {
            status = Status.Cancelled;
        }

        return status;
    }

    private async Task SaveFileAsync(StorageFile file)
    {
        using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
        {
            using (Stream stream = transaction.Stream.AsStreamForWrite())
            {
                await ViewModel.SaveAsync(stream);

                // delete any existing file data beyond the end of the stream
                transaction.Stream.Size = transaction.Stream.Position;

                await transaction.CommitAsync();
            }
        }
    }

    private async Task<Status> SaveAsync()
    {
        Status status = Status.Cancelled;

        if (sourceFile is not null)
        {
            try
            {
                await SaveFileAsync(sourceFile);
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
            status = await SaveAsAsync();
        }

        return status;
    }

    private async Task<Status> SaveAsAsync()
    {
        Status status = Status.Cancelled;

        FileSavePicker savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, parentWindow.WindowPtr);
        savePicker.FileTypeChoices.Add("Sudoku files", new List<string>() { App.cFileExt });
        savePicker.SuggestedFileName = (sourceFile is null) ? (string)Header : sourceFile.Name;

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file is not null)
        {
            try
            {
                await SaveFileAsync(file);
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
            if (Header is null)
            {
                Header = App.cNewPuzzleName;
            }

            ToolTipService.SetToolTip(this, Header);
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

    private async void ExecuteCloseOtherTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseOtherTabsAsync();
    }

    private bool CanCloseLeftTabs(object? param)
    {
        return parentWindow.CanCloseLeftTabs();
    }

    private async void ExecuteCloseLeftTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseLeftTabsAsync();
    }

    private bool CanCloseRightTabs(object? param)
    {
        return parentWindow.CanCloseRightTabs();
    }

    private async void ExecuteCloseRightTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseRightTabsAsync();
    }

    public XElement GetSessionData()
    {
        XElement root = new XElement("puzzle", new XAttribute("version", 1));

        root.Add(new XElement("title", Header));
        root.Add(new XElement("path", SourceFile?.Path));
        root.Add(new XElement("modified", IsModified));

        root.Add(ViewModel.GetPuzzleXml());

        return root;
    }

    public static bool ValidateSessionData(XElement root)
    {
        try
        {
            if ((root.Name == "puzzle") && (root.Attribute("version") is XAttribute vp) && int.TryParse(vp.Value, out int version))
            {
                if (version == 1)
                {
                    XElement? data = root.Element("title");

                    if (data is not null && !string.IsNullOrWhiteSpace(data.Value))
                    {
                        data = root.Element("path");

                        if (data is not null)
                        {
                            data = root.Element("modified");

                            if ((data is not null) && bool.TryParse(data.Value, out _))
                            {
                                data = root.Element("Sudoku");

                                if ((data is not null) && (data.Attribute("version") is XAttribute vs) && int.TryParse(vs.Value, out int sv))
                                {
                                    return sv == 2;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return false;
    }
}