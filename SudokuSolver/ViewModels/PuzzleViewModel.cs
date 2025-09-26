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
        Cells = new CellList(this);

        undoHelper = new UndoHelper();
        undoHelper.Push(model);

        UndoCommand = new RelayCommand(ExecuteUndo, CanUndo);
        RedoCommand = new RelayCommand(ExecuteRedo, CanRedo);

        CutCommand = new RelayCommand(ExecuteCut, CanCut);
        CopyCommand = new RelayCommand(ExecuteCopy, CanCopy);
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
    }

    private void UpdateView()
    {
        // update the view model's observable collection, causing a ui update
        foreach (Models.Cell modelCell in model.Cells)
        {
            int index = modelCell.Index;

            if (!modelCell.Equals(Cells[index]))
            {
                Cells[index] = new Cell(modelCell, this);
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

        private set
        {
            if (isModified != value)
            {
                isModified = value;
                NotifyPropertyChanged();
            }
        }
    }

    private bool CanUndo(object? param) => undoHelper.CanUndo;

    private void ExecuteUndo(object? param) 
    {
        model = undoHelper.PopUndo();
        UpdateView();
        IsModified = model != initialState;
    }

    private bool CanRedo(object? param) => undoHelper.CanRedo;

    private void ExecuteRedo(object? param) 
    {
        model = undoHelper.PopRedo();
        UpdateView();
        IsModified = model != initialState;
    }

    public void SelectedIndexChanged(int index, bool isSelected)
    {
        if (isSelected)
        {
            selectedIndex = index;
        }
        else if (selectedIndex == index)
        {
            selectedIndex = -1;
        }
    }

    public static bool CanCut(Cell cell)
    {
        return cell.HasValue && (cell.Origin == Origins.User) || (cell.Origin == Origins.Provided);
    }
    public static bool CanCopy(Cell cell)
    {
        return cell.HasValue && cell.ViewModel.ShowSolution;
    }

    public static bool CanPaste(Cell cell)
    {
        if (App.Instance.ClipboardHelper.HasValue)
        {
            if (cell.HasValue)
            {
                return cell.Value == App.Instance.ClipboardHelper.Value;
            }
            else
            {
                return cell.Possibles[App.Instance.ClipboardHelper.Value];
            }
        }

        return false;
    }

    private bool IsValidIndex => (selectedIndex >= 0) && (selectedIndex < Cells.Count);

    private bool CanCut(object? param) => IsValidIndex && CanCut(Cells[selectedIndex]);

    private bool CanCopy(object? param) => IsValidIndex && CanCopy(Cells[selectedIndex]);

    private bool CanPaste(object? param) => IsValidIndex && CanPaste(Cells[selectedIndex]);

    private void ExecuteCut(object? param) 
    {
        ExecuteCopy(null);
        UpdateCellForKeyDown(selectedIndex, 0); // delete
    }

    private void ExecuteCopy(object? param) 
    {
        ClipboardHelper.Copy(Cells[selectedIndex].Value);
    }

    private void ExecutePaste(object? param)
    {
        UpdateCellForKeyDown(selectedIndex, App.Instance.ClipboardHelper.Value);
    }

    private bool CanMarkProvided(object? param) => Cells.Any(c => c.Origin == Origins.User);

    private void ExecuteMarkProvided(object? param) 
    {
        model.SetOriginToProvided();
        UpdateView();
        undoHelper.Push(model);
        IsModified = model != initialState;
    }

    private bool CanClearProvided(object? param) => Cells.Any(c => c.Origin == Origins.Provided);

    private void ExecuteClearProvided(object? param)
    {
        model.SetOriginToUser();
        UpdateView();
        undoHelper.Push(model);
        IsModified = model != initialState;
    }


    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
