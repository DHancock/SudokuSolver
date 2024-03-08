using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;


internal sealed partial class PuzzleTabViewItem : TabViewItem
{
    private enum Error { Success, Failure }
    private enum Status { Cancelled, Continue }

    private PuzzleViewModel? viewModel;
    private StorageFile? sourceFile;
    private PrintHelper? printHelper;
    private ErrorDialog? errorDialog;
    private bool errorDialogOpen = false;


    public PuzzleTabViewItem()
    {
        this.InitializeComponent();

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

        Unloaded += (s, e) =>
        {
            // only animate theme changes when this is the current tab
            Puzzle.BackgroundBrushTransition.Duration = TimeSpan.Zero;
        };

        Clipboard.ContentChanged += async (s, o) =>
        {
            await ViewModel.ClipboardContentChanged();
        };
    }

    public PuzzleTabViewItem(StorageFile storageFile) : this()
    {
        sourceFile = storageFile;
        Loaded += LoadedHandler;

        static async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            PuzzleTabViewItem tab = (PuzzleTabViewItem)sender;
            tab.Loaded -= LoadedHandler;

            if (await tab.LoadFile(tab.sourceFile!) != Error.Success)
            {
                tab.sourceFile = null;
            }

            tab.UpdateTabHeader();
        }
    }

    public PuzzleTabViewItem(PuzzleTabViewItem source) : this()
    {
        ViewModel = source.ViewModel;
        sourceFile = source.sourceFile;
    }

    public PuzzleViewModel ViewModel
    {
        get => viewModel!;
        set
        {
            Debug.Assert(value is not null);
            viewModel = value;
            value.PropertyChanged += ViewModel_PropertyChanged;
            Puzzle.ViewModel = value;
        }
    }

    public void FocusLastSelectedCell()
    {
        if (!errorDialogOpen)
        {
            Puzzle.FocusLastSelectedCell();
        }
    }

    public async Task<bool> HandleTabCloseRequested()
    {
        Status status = Status.Continue;

        if (IsModified)
        {
            CloseMenuFlyouts();
            errorDialog?.Hide();

            status = await SaveExistingFirst();
        }

        if (status == Status.Cancelled)  // the save existing prompt was canceled
        {
            FocusLastSelectedCell();
            return false;
        }

        return true;
    }

    private void CloseMenuFlyouts()
    {
        foreach (Popup popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(this.XamlRoot))
        {
            if (popup.Child is MenuFlyoutPresenter)
            {
                popup.IsOpen = false;
            }
        }
    }

    public static bool IsPrintingAvailable => !IntegrityLevel.IsElevated && PrintManager.IsSupported();

    public static bool IsFileDialogAvailable => !IntegrityLevel.IsElevated;

    public bool IsModified => ViewModel!.IsModified;

    private void NewTabClickHandler(object sender, RoutedEventArgs e)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        TabViewItem tab = window.CreatePuzzleTab();
        window.AddTab(tab);
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
            InitializeWithWindow.Initialize(openPicker, App.Instance.GetWindowForElement(this).WindowPtr);
            openPicker.FileTypeFilter.Add(App.cFileExt);

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file is not null)
            {
                Error error = await LoadFile(file);

                if (error == Error.Success)
                {
                    sourceFile = file;
                    UpdateTabHeader();
                }
            }
        }

        FocusLastSelectedCell();
    }


    private void NewWindowClickHandler(object sender, RoutedEventArgs e)
    {
        _ = new MainWindow(App.Instance.GetWindowForElement(this).RestoreBounds);
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
            printHelper ??= new PrintHelper(App.Instance.GetWindowForElement(this));

            PuzzleView printView = new PuzzleView()
            {
                IsPrintView = true,
                ViewModel = ViewModel,
            };

            await printHelper.PrintViewAsync(PrintCanvas, printView, sourceFile, Settings.Data.PrintSettings.Clone());
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("A printing error occurred.", ex.Message);
        }
    }

    private async void CloseTabClickHandler(object sender, RoutedEventArgs e)
    {
        if (await HandleTabCloseRequested())
        {
            MainWindow window = App.Instance.GetWindowForElement(this);
            window.CloseTab(Parent);
        }
    }

    private void CloseWindowClickHandler(object sender, RoutedEventArgs e)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        window.PostCloseMessage();
    }

    private void ExitClickHandler(object sender, RoutedEventArgs e)
    {
        App.Instance.AttemptCloseAllWindows();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        window.AddOrSelectSettingsTab();
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
            string heading = $"An error occurred when opening {file.Name}.";
            await ShowErrorDialog(heading, ex.Message);
        }

        return error;
    }

    private async Task<Status> SaveExistingFirst()
    {
        Status status = Status.Continue;
        string path;

        if (sourceFile is null)
        {
            path = App.cNewPuzzleName;
        }
        else
        {
            path = sourceFile.Path;
        }

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
        Status status = Status.Continue;

        if (sourceFile is not null)
        {
            try
            {
                await SaveFile(sourceFile);
            }
            catch (Exception ex)
            {
                string heading = $"An error occurred when saving {sourceFile.Name}.";
                await ShowErrorDialog(heading, ex.Message);
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
        InitializeWithWindow.Initialize(savePicker, App.Instance.GetWindowForElement(this).WindowPtr);

        savePicker.FileTypeChoices.Add("Sudoku files", new List<string>() { App.cFileExt });

        if (sourceFile is null)
        {
            savePicker.SuggestedFileName = App.cNewPuzzleName;
        }
        else
        {
            savePicker.SuggestedFileName = sourceFile.Name;
        }

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file is not null)
        {
            try
            {
                await SaveFile(file);
                sourceFile = file;
                UpdateTabHeader();
                status = Status.Continue;
            }
            catch (Exception ex)
            {
                string heading = $"An error occurred when saving {file.Name}.";
                await ShowErrorDialog(heading, ex.Message);
            }
        }

        return status;
    }

    private async Task ShowErrorDialog(string message, string details)
    {
        if (errorDialog is null)
        {
            errorDialog = new ErrorDialog(XamlRoot);
            errorDialog.Closed += (s, e) =>
            {
                errorDialogOpen = false;
                FocusLastSelectedCell();
            };
        }

        errorDialogOpen = true;
        errorDialog.RequestedTheme = LayoutRoot.ActualTheme;
        errorDialog.Message = message;
        errorDialog.Details = details;
        await errorDialog.ShowAsync();
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
}