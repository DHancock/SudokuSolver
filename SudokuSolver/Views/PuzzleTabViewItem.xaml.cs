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

    private readonly PuzzleViewModel viewModel;
    private string filePath = string.Empty;
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

        Loaded += LoadedHandler;

        // size changed can also indicate that this tab has been selected and that it's content is now valid 
        Puzzle.SizeChanged += Puzzle_SizeChanged;

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

    public PuzzleTabViewItem(MainWindow parent, string path) : this(parent)
    {
        initialisationPhase += 1;

        Loaded += LoadedHandlerAsync;
        
        async void LoadedHandlerAsync(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandlerAsync;

            if (await tab.LoadFileAsync(path) == Error.Success)
            {
                filePath = path;
            }
            
            tab.UpdateTabHeader();
            tab.initialisationPhase -= 1;
        }
    }

    public PuzzleTabViewItem(MainWindow parent, XElement root) : this(parent)
    {
        initialisationPhase += 1;

        XElement? data = root.Element("title");

        if (data is not null)
        {
            HeaderText = data.Value;
        }

        data = root.Element("path");
        
        if (data is not null)
        {
            filePath = data.Value ?? string.Empty;
        }

        data = root.Element("showPossibles");

        if (data is not null)
        {
            ViewModel.ShowPossibles = data.Value == "true";
        }

        data = root.Element("showSolution");

        if (data is not null)
        {
            ViewModel.ShowSolution = data.Value == "true";
        }

        data = root.Element("Sudoku");

        if (data is not null)
        {
            ViewModel.LoadXml(data, isFileBacked: false);
        }

        if (string.IsNullOrEmpty(filePath))
        {
            initialisationPhase -= 1;
        }
        else
        {
            Loaded += LoadedHandler;  // avoid any delay creating the tab
        }

        async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            Loaded -= LoadedHandler;

            try
            {
                // load the mirrored backing file to update the modified flag and set the initial state
                await using (FileStream fs = File.OpenRead(filePath))
                {
                    XDocument document = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
                    ViewModel.ProcessBackingFileData(document.Root);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(ex is FileNotFoundException or DirectoryNotFoundException);

                filePath = string.Empty;
                ViewModel.IsModified = true; // it may now need to be saved
            }

            initialisationPhase -= 1;
        }
    }

    public PuzzleTabViewItem(MainWindow parent, PuzzleTabViewItem source) : this(parent, source.GetSessionData())
    {
        viewModel.TransferUndoHistory(source.viewModel);
    }

    private void Puzzle_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        parentWindow.SetWindowDragRegions();
    }

    public void Closed()
    {
        FileMenuItem.Unloaded -= MenuItem_Unloaded;
        ViewMenuItem.Unloaded -= MenuItem_Unloaded;
        EditMenuItem.Unloaded -= MenuItem_Unloaded;

        Puzzle.SizeChanged -= Puzzle_SizeChanged;
        Puzzle.DragEnter -= Puzzle_DragEnter;
        Puzzle.Drop -= Puzzle_Drop;

        viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        Puzzle.Closed();
    }

    private void MenuItem_Unloaded(object sender, RoutedEventArgs e)
    {
        FocusSelectedCell();
    }

    private async void Puzzle_Drop(object sender, DragEventArgs e)
    {
        IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

        foreach (IStorageItem item in items)
        {
            if (IsValidStorgeItem(item))
            {
                if (parentWindow.IsOpenInExistingTab(item.Path))
                {
                    parentWindow.SwitchToTab(item.Path);
                }
                else
                {
                    parentWindow.AddTab(new PuzzleTabViewItem(parentWindow, item.Path));
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

    public PuzzleViewModel ViewModel => viewModel;

    public string HeaderText
    {
        get => ((TextBlock)Header).Text;
        private set => ((TextBlock)Header).Text = value;
    }

    public string SourceFile => filePath;

    public void FocusSelectedCell()
    {
        if (IsLoaded && IsSelected && parentWindow.IsActive && !parentWindow.ContentDialogHelper.IsContentDialogOpen)
        {
            Puzzle.FocusSelectedCell();
        }
    }

    public async Task<bool> SaveTabContentsAsync()
    {
        Debug.Assert(IsModified);
        return await SaveExistingFirstAsync() != Status.Cancelled;
    }

    public void EnableAccessKeys(bool enable)
    {
        // disable when a content dialog is shown
        foreach (MenuBarItem mbi in Menu.Items)
        {
            mbi.IsEnabled = enable;
        }

        SettingsButtton.IsEnabled = enable;
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
                if (parentWindow.IsOpenInExistingTab(file.Path))
                {
                    parentWindow.SwitchToTab(file.Path);
                }
                else
                {
                    Error error = await LoadFileAsync(file.Path);

                    if (error == Error.Success)
                    {
                        filePath = file.Path;
                        UpdateTabHeader();
                        AddToRecentFilesJumpList();
                    }
                }
            }
        }
        
        FocusSelectedCell();
    }

    private unsafe void AddToRecentFilesJumpList()
    {
        Debug.Assert(!string.IsNullOrEmpty(filePath));

        if (!string.IsNullOrEmpty(filePath))
        {
            fixed (char* lpStringLocal = filePath)
            {
                PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_PATHW, lpStringLocal);
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
            await parentWindow.PrintPuzzleAsync(GetSessionData());
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

    private async Task<Error> LoadFileAsync(string path)
    {
        Error error = Error.Failure;

        try
        {
            await using (FileStream fs = File.OpenRead(path))
            {
                XDocument document = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
                ViewModel.LoadXml(document.Root, isFileBacked: true);
                error = Error.Success;
            }
        }
        catch (Exception ex)
        {
            FileOpenErrorDialog? dialog = parentWindow.ContentDialogHelper.GetFileOpenErrorDialog();

            if (dialog is not null)
            {
                // if the user drags and drops files which have more than one error
                dialog.AddError(Path.GetFileName(path), ex.Message);
            }
            else
            {
                await parentWindow.ContentDialogHelper.ShowFileOpenErrorDialogAsync(this, Path.GetFileName(path), ex.Message);
            }
        }

        return error;
    }

    private async Task<Status> SaveExistingFirstAsync()
    {
        Status status = Status.Continue;
        string path = string.IsNullOrEmpty(filePath) ? HeaderText : filePath;

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

    private async Task SaveFileAsync(string path)
    {
        await using (Stream stream = File.Open(path, FileMode.Create))
        {
            await ViewModel.SaveAsync(stream);
        }
    }

    private async Task<Status> SaveAsync()
    {
        Status status = Status.Cancelled;

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                await SaveFileAsync(filePath);
                status = Status.Continue;
            }
            catch (Exception ex)
            {
                string template = App.Instance.ResourceLoader.GetString("FileSaveErrorTemplate");
                string heading = string.Format(template, Path.GetFileName(filePath));
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
        savePicker.SuggestedFileName = string.IsNullOrEmpty(filePath) ? HeaderText : Path.GetFileName(filePath);

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file is not null)
        {
            try
            {
                await SaveFileAsync(file.Path);
                filePath = file.Path;
                UpdateTabHeader();
                AddToRecentFilesJumpList();
                status = Status.Continue;
            }
            catch (Exception ex)
            {
                string template = App.Instance.ResourceLoader.GetString("FileSaveErrorTemplate");
                string heading = string.Format(template, Path.GetFileName(file.Path));
                await parentWindow.ContentDialogHelper.ShowErrorDialogAsync(this, heading, ex.Message);
            }
        }

        Puzzle.FocusSelectedCell();

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
        if (string.IsNullOrEmpty(filePath))
        {
            if (string.IsNullOrEmpty(HeaderText))
            {
                HeaderText = parentWindow.MakeUniqueHeaderText();
            }

            ToolTipService.SetToolTip(this, HeaderText);
        }
        else
        {
            HeaderText = Path.GetFileName(filePath);
            ToolTipService.SetToolTip(this, filePath);
        }

        if (IsModified)
        {
            IconSource ??= new SymbolIconSource() { Symbol = Symbol.Edit, };
        }
        else
        {
            IconSource = null;
        }
    }

    private bool CanRenameTab(object? param) => string.IsNullOrEmpty(filePath);

    private async void ExecuteRenameTabCommand(object? param)
    {
        (ContentDialogResult Result, string NewName) = await parentWindow.ContentDialogHelper.ShowRenameTabDialogAsync(this, HeaderText);

        if (Result == ContentDialogResult.Primary)
        {
            HeaderText = NewName;
            UpdateTabHeader();
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
        root.Add(new XElement("path", filePath));
        root.Add(new XElement("modified", IsModified));

        // add the optional "per view" settings in release 1.13.0
        // the version hasn't been incremented for backwards compatibility
        // they don't conflict with any other puzzle tab session data
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
                            // the following two are new but optional
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
                                return (sv == 2) || (sv == 3);
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

    public void InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if ((modifiers == VirtualKeyModifiers.Shift) && (key == VirtualKey.F10))
        {
            if ((FocusManager.GetFocusedElement(XamlRoot) is TabViewItem) || !Puzzle.ShowCellContextMenu())
            {
                ((MenuFlyout)ContextFlyout).ShowAt((FrameworkElement)Header);
            }                                                
        }
        else
        {
            if ((modifiers == VirtualKeyModifiers.Shift) && (key == VirtualKey.Insert))
            {
                modifiers = VirtualKeyModifiers.Control;
                key = VirtualKey.V;
            }

            foreach (MenuBarItem mbi in Menu.Items)
            {
                if (mbi.IsEnabled && Utils.InvokeMenuItemForKeyboardAccelerator(mbi.Items, modifiers, key))
                {
                    return;
                }
            }
        }

        Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)ContextFlyout).Items, modifiers, key);
    }
}
