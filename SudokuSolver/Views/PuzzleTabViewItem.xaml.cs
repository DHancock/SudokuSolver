using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;


internal sealed partial class PuzzleTabViewItem : TabViewItem, ITabItem, ISession
{
    private enum Error { Success, Failure }
    private enum Status { Cancelled, Continue }

    private RelayCommand RenameTabCommand { get; }
    private RelayCommand CloseOtherTabsCommand { get; }
    private RelayCommand CloseLeftTabsCommand { get; }
    private RelayCommand CloseRightTabsCommand { get; }

    private PuzzleViewModel? viewModel;
    private StorageFile? sourceFile;
    private readonly MainWindow parentWindow;
    private int initialisationPhase = 0;

    public PuzzleTabViewItem(MainWindow parent)
    {
        this.InitializeComponent();
        initialisationPhase += 1;

        parentWindow = parent;

        viewModel = new PuzzleViewModel();
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Puzzle.ViewModel = viewModel;

        FileMenuItem.Unloaded += MenuItem_Unloaded;
        ViewMenuItem.Unloaded += MenuItem_Unloaded;
        EditMenuItem.Unloaded += MenuItem_Unloaded;

        Clipboard.ContentChanged += Clipboard_ContentChanged;
        GotFocus += PuzzleTabViewItem_GotFocus;
        Loaded += LoadedHandler;

        // size changed can also indicate that this tab has been selected and that it's content is now valid 
        Puzzle.SizeChanged += Puzzle_SizeChanged;

        EnableKeyboardAccelerators(enable: false);

        RenameTabCommand = new RelayCommand(ExecuteRenameTabCommand, CanRenameTab);
        CloseOtherTabsCommand = new RelayCommand(ExecuteCloseOtherTabsAsync, CanCloseOtherTabs);
        CloseLeftTabsCommand = new RelayCommand(ExecuteCloseLeftTabsAsync, CanCloseLeftTabs);
        CloseRightTabsCommand = new RelayCommand(ExecuteCloseRightTabsAsync, CanCloseRightTabs);

        if (!IntegrityLevel.IsElevated)
        {
            Puzzle.AllowDrop = true;
            Puzzle.DragEnter += Puzzle_DragEnter;
            Puzzle.Drop += Puzzle_Drop;
        }

        static void LoadedHandler(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandler;

            // set for the next theme transition
            tab.Puzzle.BackgroundBrushTransition.Duration = TimeSpan.FromMilliseconds(250);
            tab.UpdateTabHeader();

            Button? closeButton = tab.FindChild<Button>("CloseButton");
            Debug.Assert(closeButton is not null);

            if (closeButton is not null)
            {
                string text = App.Instance.ResourceLoader.GetString("CloseTabToolTip");
                ToolTipService.SetToolTip(closeButton, text);
            }

            tab.initialisationPhase -= 1;
        }
    }

    private void Puzzle_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        parentWindow.SetWindowDragRegions();
    }

    public PuzzleTabViewItem(MainWindow parent, StorageFile storageFile) : this(parent)
    {
        initialisationPhase += 1;

        Loaded += LoadedHandlerAsync;
        
        async void LoadedHandlerAsync(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandlerAsync;

            if (await tab.LoadFileAsync(storageFile) == Error.Success)
            {
                tab.sourceFile = storageFile;
            }
            
            tab.UpdateTabHeader();

            tab.initialisationPhase -= 1;
        }
    }

    public PuzzleTabViewItem(MainWindow parent, XElement root) : this(parent)
    {
        initialisationPhase += 1;

        Loaded += LoadedHandlerAsync;

        async void LoadedHandlerAsync(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandlerAsync;
            bool forceModified = false;
            XElement? data = root.Element("path");

            if ((data is not null) && !string.IsNullOrEmpty(data.Value))
            {
                try
                {
                    tab.sourceFile = await StorageFile.GetFileFromPathAsync(data.Value);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{data.Value} - {ex}");

                    // indicate that it may need to be resaved
                    forceModified = true;
                }
            }

            data = root.Element("title");

            if (data is not null)
            {
                tab.HeaderText = data.Value;
            }

            data = root.Element("modified");
            bool isModified = false;

            if (data is not null)
            {
                isModified = data.Value == "true";
            }

            data = root.Element("showPossibles");

            if (data is not null)
            {
                tab.ViewModel.ShowPossibles = data.Value == "true";
            }

            data = root.Element("showSolution");

            if (data is not null)
            {
                tab.ViewModel.ShowSolution = data.Value == "true";
            }

            data = root.Element("Sudoku");

            if (data is not null)
            {
                tab.ViewModel.LoadXml(data, isModified || forceModified);
            }

            tab.UpdateTabHeader();

            tab.initialisationPhase -= 1;
        }
    }

    public void Closed()
    {
        // the tab's keyboard accelerators would still
        // be active (until presumably it's garbage collected)
        EnableKeyboardAccelerators(enable: false);

        FileMenuItem.Unloaded -= MenuItem_Unloaded;
        ViewMenuItem.Unloaded -= MenuItem_Unloaded;
        EditMenuItem.Unloaded -= MenuItem_Unloaded;
        GotFocus -= PuzzleTabViewItem_GotFocus;
        Puzzle.SizeChanged -= Puzzle_SizeChanged;
        Puzzle.DragEnter -= Puzzle_DragEnter;
        Puzzle.Drop -= Puzzle_Drop;

        Clipboard.ContentChanged -= Clipboard_ContentChanged;

        if (viewModel is not null)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            viewModel = null;
        }

        Puzzle.Closed();
    }

    private static void PuzzleTabViewItem_GotFocus(object sender, RoutedEventArgs e)
    {
        PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
        tab.FocusLastSelectedCell();
    }

    private void MenuItem_Unloaded(object sender, RoutedEventArgs e)
    {
        FocusLastSelectedCell();
    }

    private async void Clipboard_ContentChanged(object? sender, object e)
    {
        await ViewModel.ClipboardContentChangedAsync();
    }

    private async void Puzzle_Drop(object sender, DragEventArgs e)
    {
        IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

        foreach (IStorageItem item in items)
        {
            if (IsValidStorgeItem(item))
            {
                if (parentWindow.IsOpenInExistingTab((StorageFile)item))
                {
                    parentWindow.SwitchToTab((StorageFile)item);
                }
                else
                {
                    parentWindow.AddTab(new PuzzleTabViewItem(parentWindow, (StorageFile)item));
                }
            }
        }
    }

    private void Puzzle_DragEnter(object sender, DragEventArgs e)
    {
        try
        {
            if (e.DataView.Contains("FileDrop"))
            {
                IReadOnlyList<IStorageItem> items = e.DataView.GetStorageItemsAsync().GetAwaiter().GetResult();

                foreach (IStorageItem item in items)
                {
                    if (IsValidStorgeItem(item))
                    {
                        e.DragUIOverride.IsGlyphVisible = false;
                        e.DragUIOverride.IsCaptionVisible = false;
                        e.DragUIOverride.SetContentFromBitmapImage(AboutBox.GetImage(ActualTheme));

                        e.AcceptedOperation = DataPackageOperation.Copy;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private static bool IsValidStorgeItem(IStorageItem item)
    {
        return item.IsOfType(StorageItemTypes.File) &&
                App.cFileExt.Equals(Path.GetExtension(item.Path), StringComparison.OrdinalIgnoreCase);
    }

    public PuzzleViewModel ViewModel
    {
        get
        {
            Debug.Assert(viewModel is not null);
            return viewModel;
        }
    }

    public string HeaderText
    {
        get => ((TextBlock)Header).Text;
        private set => ((TextBlock)Header).Text = value;
    }

    public StorageFile? SourceFile => sourceFile;

    public void FocusLastSelectedCell()
    {
        if (IsLoaded && IsSelected && parentWindow.IsActive && !parentWindow.ContentDialogHelper.IsContentDialogOpen)
        {
            Puzzle.FocusLastSelectedCell();
        }
    }

    public async Task<bool> SaveTabContentsAsync()
    {
        Debug.Assert(IsModified);
        return await SaveExistingFirstAsync() != Status.Cancelled;
    }

    public void EnableKeyboardAccelerators(bool enable)
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

        UIElement[] elements = [DuplicateMenuItem, RenameMenuItem];

        foreach (UIElement element in elements)
        {
            foreach (KeyboardAccelerator ka in element.KeyboardAccelerators)
            {
                ka.IsEnabled = enable;
            }
        }
    }

    public void EnableMenuAccessKeys(bool enable)
    {
        foreach (MenuBarItem mbi in Menu.Items)
        {
            mbi.IsEnabled = enable;
        }
    }

    public static bool IsPrintingAvailable => !IntegrityLevel.IsElevated && PrintManager.IsSupported();

    public static bool IsFileDialogAvailable => !IntegrityLevel.IsElevated;

    public bool IsModified => ViewModel.IsModified;

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
            InitializeWithWindow.Initialize(openPicker, parentWindow.WindowHandle);
            openPicker.FileTypeFilter.Add(App.cFileExt);

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file is not null)
            {
                if (parentWindow.IsOpenInExistingTab(file))
                {
                    parentWindow.SwitchToTab(file);
                }
                else
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
        window.Activate();
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
            string heading = App.Instance.ResourceLoader.GetString("PrintErrorHeading");
            await parentWindow.ContentDialogHelper.ShowErrorDialogAsync(this, heading, ex.Message);
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
            FileOpenErrorDialog? dialog = parentWindow.ContentDialogHelper.GetFileOpenErrorDialog();

            if (dialog is not null)
            {
                // if the user drags and drops files which have more than one error
                dialog.AddError(file.Name, ex.Message);
            }
            else
            {
                await parentWindow.ContentDialogHelper.ShowFileOpenErrorDialogAsync(this, file.Name, ex.Message);
            }
        }

        return error;
    }

    private async Task<Status> SaveExistingFirstAsync()
    {
        Status status = Status.Continue;
        string path = (sourceFile is null) ? HeaderText : sourceFile.Path;

        ContentDialogResult result = await parentWindow.ContentDialogHelper.ShowConfirmSaveDialogAsync(this, path);

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
                string template = App.Instance.ResourceLoader.GetString("FileSaveErrorTemplate");
                string heading = string.Format(template, sourceFile.Name);
                await parentWindow.ContentDialogHelper.ShowErrorDialogAsync(this, heading, ex.Message);
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
        InitializeWithWindow.Initialize(savePicker, parentWindow.WindowHandle);
        string fileChoice = App.Instance.ResourceLoader.GetString("SavePickerFileChoice");
        savePicker.FileTypeChoices.Add(fileChoice, new List<string>() { App.cFileExt });
        savePicker.SuggestedFileName = (sourceFile is null) ? HeaderText : sourceFile.Name;

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
                string template = App.Instance.ResourceLoader.GetString("FileSaveErrorTemplate");
                string heading = string.Format(template, file.Name);
                await parentWindow.ContentDialogHelper.ShowErrorDialogAsync(this, heading, ex.Message);
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
            if (string.IsNullOrEmpty(HeaderText))
            {
                HeaderText = parentWindow.MakeUniqueHeaderText();
            }

            ToolTipService.SetToolTip(this, HeaderText);
        }
        else
        {
            HeaderText = sourceFile.Name;
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


    private bool CanRenameTab(object? param)
    {
        return !parentWindow.ContentDialogHelper.IsContentDialogOpen && (sourceFile is null);
    }

    private async void ExecuteRenameTabCommand(object? param)
    {
        if (CanRenameTab(null))
        {
            (ContentDialogResult Result, string NewName) = await parentWindow.ContentDialogHelper.ShowRenameTabDialogAsync(this, HeaderText);

            if (Result == ContentDialogResult.Primary)
            {
                HeaderText = NewName;
                UpdateTabHeader();
            }
        }
    }

    private bool CanCloseOtherTabs(object? param)
    {
        return parentWindow.CanCloseOtherTabs();
    }

    private async void ExecuteCloseOtherTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseOtherTabsAsync(this);
    }

    private bool CanCloseLeftTabs(object? param)
    {
        return parentWindow.CanCloseLeftTabs(this);
    }

    private async void ExecuteCloseLeftTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseLeftTabsAsync(this);
    }

    private bool CanCloseRightTabs(object? param)
    {
        return parentWindow.CanCloseRightTabs(this);
    }

    private async void ExecuteCloseRightTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseRightTabsAsync(this);
    }

    public XElement GetSessionData()
    {
        XElement root = new XElement("puzzle", new XAttribute("version", 1));

        root.Add(new XElement("title", HeaderText));
        root.Add(new XElement("path", SourceFile?.Path));
        root.Add(new XElement("modified", IsModified));

        // add the optional "per view" settings in release 1.13.0
        // the version hasn't been incremented for backwards compatibility
        // they don't confict with any other puzzle tab session data
        root.Add(new XElement("showPossibles", ViewModel.ShowPossibles));
        root.Add(new XElement("showSolution", ViewModel.ShowSolution));

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
                                // the follwing two are new but optional
                                data = root.Element("showPossibles");

                                if ((data is not null) && !bool.TryParse(data.Value, out _))
                                {
                                    return false;
                                }

                                data = root.Element("showSolution");

                                if ((data is not null) && !bool.TryParse(data.Value, out _))
                                {
                                    return false;
                                }

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

    private void DuplicateTabClickHandler(object sender, RoutedEventArgs e)
    {
        if (initialisationPhase > 0)   // on hot key repeat, don't duplicate a partially initialised tab
            return;

        XElement root = GetSessionData();
        SetElement(root, "path", string.Empty);
        SetElement(root, "modified", PuzzleHasData(root) ? "true" : "false");
        SetElement(root, "title", string.Empty);

        TabViewItem tab = new PuzzleTabViewItem(parentWindow, root);
        parentWindow.AddTab(tab, parentWindow.IndexOf(this) + 1);
        
        static void SetElement(XElement root, string name, string value)
        {
            XElement? data = root.Element(name);

            if (data is not null)
            {
                data.Value = value;
            }
        }

        static bool PuzzleHasData(XElement root)
        {
            XElement? s = root.Element("Sudoku");
            XElement? c = s?.Element("Cell");
            return c is not null;
        }
    }

    public int PassthroughCount => 5;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        Debug.Assert(rects.Length >= PassthroughCount);

        rects[0] = Utils.GetPassthroughRect(FileMenu);
        rects[1] = Utils.GetPassthroughRect(EditMenu);
        rects[2] = Utils.GetPassthroughRect(ViewMenu);
        rects[3] = Utils.GetPassthroughRect(SettingsButtton);
        rects[4] = Utils.GetPassthroughRect(Puzzle);
    }
}
