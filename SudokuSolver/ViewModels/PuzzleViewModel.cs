using SudokuSolver.Utilities;
using SudokuSolver.Common;
using SudokuSolver.Models;

namespace SudokuSolver.ViewModels;

internal sealed class PuzzleViewModel : INotifyPropertyChanged
{
    private const int cMaxUndoCount = 10;
    public CellList Cells { get; }

    private readonly UndoHelper undoHelper;
    private readonly Settings.PerViewSettings viewSettings;

    private PuzzleModel model;
    private bool isModified = false;
    private int clipboardValue = 0;
    private int selectedIndex = -1;

    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }
    public RelayCommand CutCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand PasteCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand MarkAsGivenCommand { get; }

    public PuzzleViewModel(Settings.PerViewSettings perViewSettings)
    {
        model = new PuzzleModel();
        Cells = new CellList();
        viewSettings = perViewSettings;

        undoHelper = new UndoHelper(cMaxUndoCount);
        undoHelper.Add(model);

        UndoCommand = new RelayCommand(ExecuteUndo, CanUndo);
        RedoCommand = new RelayCommand(ExecuteRedo, CanRedo);

        CutCommand = new RelayCommand(ExecuteCut, CanCutCopyDelete);
        CopyCommand = new RelayCommand(ExecuteCopy, CanCutCopyDelete);
        PasteCommand = new RelayCommand(ExecutePaste, CanPaste);
        DeleteCommand = new RelayCommand(ExecuteDelete, CanCutCopyDelete);

        MarkAsGivenCommand = new RelayCommand(ExecuteMarkAsGiven, CanMarkAsGiven);
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
                return model.Add;

            if (currentValue != newValue)
            {
                if ((cell.Origin == Origins.User) || (cell.Origin == Origins.Given))
                    return model.Edit;

                if ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated))
                    return model.EditForced;
            }

            if ((currentValue == newValue) && ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated)))
                return model.SetOrigin;
        }

        if ((newValue == 0) && (currentValue > 0) && ((cell.Origin == Origins.User) || (cell.Origin == Origins.Given)))
            return model.Delete;

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
                undoHelper.Add(model);
                IsModified = true;
                RaiseCanExecuteChanged();
            }
            else
            {
                Utils.PlayExclamation();
            }
        }
    }

    public void Save(Stream stream)
    {
        model.Save(stream);
        IsModified = false;
    }

    public void Open(Stream stream)
    {
        try
        {
            model.Clear();
            model.Open(stream);
            IsModified = false;
        }
        catch
        {
            model.Clear();
            throw;
        }
        finally
        {
            UpdateView();
            undoHelper.Add(model);
            RaiseCanExecuteChanged();
        }
    }

    private void UpdateView()
    {
        // update the view model's observable collection, causing a ui update
        foreach (Models.Cell modelCell in model.Cells)
        {
            int index = modelCell.Index;

            if (!modelCell.Equals(Cells[index]))
                Cells[index] = new Cell(modelCell);
        }

        Debug.Assert(model.CompletedCellCountIsValid);
    }

    public void New()
    {
        model.Clear();
        UpdateView();
        undoHelper.Add(model);
        IsModified = false;
        RaiseCanExecuteChanged();
    }

    public bool ShowPossibles
    {
        get => viewSettings.ShowPossibles;
        set
        {
            if (viewSettings.ShowPossibles != value)
            {
                viewSettings.ShowPossibles = value;
                Settings.Data.ViewSettings.ShowPossibles = value;
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
                Settings.Data.ViewSettings.ShowSolution = value;
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
                Cells[index] = new Cell(cell);
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
            model = undoHelper.UndoPop();
            UpdateView();
            IsModified = true;
            RaiseCanExecuteChanged();
        }
    }


    public bool CanRedo(object? param = null) => undoHelper.CanRedo;

    public void ExecuteRedo(object? param) 
    {
        if (CanRedo())
        {
            model = undoHelper.RedoPop();
            UpdateView();
            IsModified = true;
            RaiseCanExecuteChanged();
        }
    }

    public void Puzzle_SelectedIndexChanged(object sender, int e)
    {
        selectedIndex = e;
        RaiseCanExecuteChanged();
    }

    private void RaiseCanExecuteChanged()
    {
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        CutCommand.RaiseCanExecuteChanged();
        CopyCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        PasteCommand.RaiseCanExecuteChanged();
        MarkAsGivenCommand.RaiseCanExecuteChanged();
    }

    private bool CanCutCopyDelete(object? param = null) => (selectedIndex >= 0) && Cells[selectedIndex].HasValue;

    private bool CanPaste(object? param = null) => (selectedIndex >= 0) && (clipboardValue > 0);

    public async Task ClipboardContentChanged()
    {
        clipboardValue = await ReadClipboardNumber();
        PasteCommand.RaiseCanExecuteChanged();
    }

    private static async Task<int> ReadClipboardNumber()
    {
        try
        {
            DataPackageView dpv = Clipboard.GetContent();

            if (dpv.AvailableFormats.Contains(StandardDataFormats.Text))
            {
                string data = await dpv.GetTextAsync();

                if (int.TryParse(data, out int number) && number > 0 && number < 10)
                    return number;
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
            UpdateCellForKeyDown(selectedIndex, clipboardValue);
    }
    
    public void ExecuteDelete(object? param = null)
    {
        if (CanCutCopyDelete())
            UpdateCellForKeyDown(selectedIndex, 0);
    }

    public bool CanMarkAsGiven(object? param = null) => Cells.Any(c => c.Origin == Origins.User);

    public void ExecuteMarkAsGiven(object? param) 
    {
        if (CanMarkAsGiven())
        {
            model.SetOriginToGiven();
            UpdateView();
            IsModified = true;
        }
    }


    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
