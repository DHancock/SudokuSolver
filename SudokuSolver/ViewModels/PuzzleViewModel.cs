using SudokuSolver.Common;
using SudokuSolver.Models;
using SudokuSolver.Utilities;

namespace SudokuSolver.ViewModels;

internal sealed partial class PuzzleViewModel : INotifyPropertyChanged
{
    public CellList Cells { get; }

    private readonly UndoHelper undoHelper;

    private PuzzleModel model;
    private PuzzleModel initialState;
    private bool isModified = false;
    private int clipboardValue = 0;
    private int selectedIndex = -1;

    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }
    public RelayCommand CutCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand PasteCommand { get; }
    public RelayCommand MarkProvidedCommand { get; }
    public RelayCommand ClearProvidedCommand { get; }

    private readonly Settings.PerViewSettings viewSettings;


    public PuzzleViewModel()
    {
        this.viewSettings = Settings.Instance.ViewSettings.Clone();

        model = new PuzzleModel();
        initialState = new PuzzleModel();
        Cells = new CellList();

        undoHelper = new UndoHelper();
        undoHelper.Push(model);

        UndoCommand = new RelayCommand(ExecuteUndo, CanUndo);
        RedoCommand = new RelayCommand(ExecuteRedo, CanRedo);

        CutCommand = new RelayCommand(ExecuteCut, CanCutCopyDelete);
        CopyCommand = new RelayCommand(ExecuteCopy, CanCutCopyDelete);
        PasteCommand = new RelayCommand(ExecutePaste, CanPaste);

        MarkProvidedCommand = new RelayCommand(ExecuteMarkProvided, CanMarkProvided);
        ClearProvidedCommand = new RelayCommand(ExecuteClearProvided, CanClearProvided);
    }

    // an empty implementation used to indicate no action is required
    private static bool NoOp(int index, int value) => throw new NotImplementedException();

    private Func<int, int, bool> DetermineChange(int index, int newValue)
    {
        Cell cell = Cells[index];
        int currentValue = cell.Value;
        Origins currentOrigin = cell.Origin;

        if (newValue > 0)
        {
            if (currentValue == 0)
            {
                return model.Add;
            }

            if (currentValue != newValue)
            {
                if ((cell.Origin == Origins.User) || (cell.Origin == Origins.Provided))
                {
                    return model.Edit;
                }

                if ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated))
                {
                    return model.EditForced;
                }
            }

            if ((currentValue == newValue) && ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated)))
            {
                return model.SetOrigin;
            }
        }

        if ((newValue == 0) && (currentValue > 0) && ((cell.Origin == Origins.User) || (cell.Origin == Origins.Provided)))
        {
            return model.Delete;
        }

        // typical changes that require no action are deleting an empty cell, 
        // deleting a cell containing a calculated value which would then just 
        // be recalculated and also editing a user cell to have the same value.
        return NoOp;
    }

    // the user typed a value into a cell
    public void UpdateCellForKeyDown(int index, int newValue)
    {
        Func<int, int, bool> modelFunction = DetermineChange(index, newValue);

        if (modelFunction != NoOp)
        {
            if (modelFunction(index, newValue))
            {
                UpdateView();
                undoHelper.Push(model);
                IsModified = model != initialState;
                UpdateMenuItemsDisabledState();
            }
            else
            {
                Utils.PlayExclamation();
            }
        }
    }

    public XElement GetPuzzleXml() => model.GetPuzzleXml();

    public async Task SaveAsync(Stream stream)
    {
        await model.SaveAsync(stream);
        initialState = new PuzzleModel(model);
        IsModified = false;
    }

    public void LoadXml(XElement? root, bool isModified)
    {
        PuzzleModel newModel = new PuzzleModel();

        try
        {
            newModel.LoadXml(root);
        }
        catch
        {
            throw;
        }

        model = newModel;
        initialState = new PuzzleModel(model);
        IsModified = isModified;
        undoHelper.Reset();
        undoHelper.Push(model);

        UpdateView();
        UpdateMenuItemsDisabledState();
    }

    private void UpdateView()
    {
        // update the view model's observable collection, causing a ui update
        foreach (Models.Cell modelCell in model.Cells)
        {
            int index = modelCell.Index;

            if (!modelCell.Equals(Cells[index]))
            {
                Cells[index] = new Cell(modelCell);
            }
        }

        Debug.Assert(model.CompletedCellCountIsValid);
    }

    public bool ShowPossibles
    {
        get => viewSettings.ShowPossibles;
        set
        {
            if (viewSettings.ShowPossibles != value)
            {
                viewSettings.ShowPossibles = value;
                UpdateViewForShowPossiblesStateChange();
                NotifyPropertyChanged();
            }
        }
    }
   
    public bool ShowSolution
    {
        get => viewSettings.ShowSolution;
        set
        {
            if (viewSettings.ShowSolution != value)
            {
                viewSettings.ShowSolution = value;
                UpdateViewForShowSolutionStateChange();
                NotifyPropertyChanged();
            }
        }
    }

    private void UpdateViewForShowPossiblesStateChange()
    {
        UpdateViewWhere(cell => !cell.HasValue);
    }

    private void UpdateViewForShowSolutionStateChange()
    {
        UpdateViewWhere(cell => cell.Origin == Origins.Calculated || cell.Origin == Origins.Trial);
    }

    private void UpdateViewWhere(Func<Cell, bool> predicate)
    {
        for (int index = 0; index < Cells.Count; index++)
        {
            Cell cell = Cells[index];

            if (predicate(cell))
            {
                Cells[index] = new Cell(cell);
            }
        }
    }

    public bool IsModified
    {
        get => isModified;
        set
        {
            if (isModified != value)
            {
                isModified = value;
                NotifyPropertyChanged();
            }
        }
    }

    public bool CanUndo(object? param = null) => undoHelper.CanUndo;

    public void ExecuteUndo(object? param) 
    {
        if (CanUndo())
        {
            model = undoHelper.PopUndo();
            UpdateView();
            IsModified = model != initialState;
            UpdateMenuItemsDisabledState();
        }
    }

    public bool CanRedo(object? param = null) => undoHelper.CanRedo;

    public void ExecuteRedo(object? param) 
    {
        if (CanRedo())
        {
            model = undoHelper.PopRedo();
            UpdateView();
            IsModified = model != initialState;
            UpdateMenuItemsDisabledState();
        }
    }

    public void Puzzle_SelectedIndexChanged(object sender, Views.Cell.SelectionChangedEventArgs e)
    {
        if (e.IsSelected)
        {
            selectedIndex = e.Index;
        }
        else if (selectedIndex == e.Index)
        {
            selectedIndex = -1;
        }

        UpdateMenuItemsDisabledState();
    }

    private void UpdateMenuItemsDisabledState()
    {
        // an unfortunate work around for https://github.com/microsoft/microsoft-ui-xaml/issues/8894
        // While the CanExecute method is automatically called when the menu is shown, the menu item
        // enabled state needs updating for the accelerator key to work while the menu is closed.
        Task.Run(() =>
        {
            UndoCommand.RaiseCanExecuteChanged();
            RedoCommand.RaiseCanExecuteChanged();
            CutCommand.RaiseCanExecuteChanged();
            CopyCommand.RaiseCanExecuteChanged();
            PasteCommand.RaiseCanExecuteChanged();
        });
    }

    private bool CanCutCopyDelete(object? param = null) => (selectedIndex >= 0) && Cells[selectedIndex].HasValue;

    private bool CanPaste(object? param = null) => (selectedIndex >= 0) && (clipboardValue > 0);

    public async Task ClipboardContentChangedAsync()
    {
        clipboardValue = await ReadClipboardNumberAsync();
        PasteCommand.RaiseCanExecuteChanged();
    }

    private static async Task<int> ReadClipboardNumberAsync()
    {
        try
        {
            DataPackageView dpv = Clipboard.GetContent();

            if (dpv.AvailableFormats.Contains(StandardDataFormats.Text))
            {
                string data = await dpv.GetTextAsync();

                if (int.TryParse(data, out int number) && number > 0 && number < 10)
                {
                    return number;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }

        return 0;
    }


    public void ExecuteCut(object? param) 
    {
        if (CanCutCopyDelete())
        {
            ExecuteCopy();
            ExecuteDelete();
        }
    }

    public void ExecuteCopy(object? param = null) 
    {
        if (CanCutCopyDelete())
        {
            DataPackage dp = new DataPackage();
            dp.SetText(Cells[selectedIndex].Value.ToString());
            Clipboard.SetContent(dp);
        }
    }

    public void ExecutePaste(object? param)
    {
        if (CanPaste())
        {
            UpdateCellForKeyDown(selectedIndex, clipboardValue);
        }
    }
    
    public void ExecuteDelete()
    {
        if (CanCutCopyDelete())
        {
            UpdateCellForKeyDown(selectedIndex, 0);
        }
    }

    public bool CanMarkProvided(object? param = null) => Cells.Any(c => c.Origin == Origins.User);

    public void ExecuteMarkProvided(object? param) 
    {
        if (CanMarkProvided())
        {
            model.SetOriginToProvided();
            UpdateView();
            IsModified = model != initialState;
        }
    }

    public bool CanClearProvided(object? param = null) => Cells.Any(c => c.Origin == Origins.Provided);

    public void ExecuteClearProvided(object? param)
    {
        if (CanClearProvided())
        {
            model.SetOriginToUser();
            UpdateView();
            IsModified = model != initialState;
        }
    }


    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
