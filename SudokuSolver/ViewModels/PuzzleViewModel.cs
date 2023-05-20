using SudokuSolver.Utilities;
using SudokuSolver.Common;
using SudokuSolver.Models;

namespace SudokuSolver.ViewModels;

internal sealed class PuzzleViewModel : INotifyPropertyChanged
{
    private PuzzleModel Model { get; }
    public CellList Cells { get; }
    public Settings.PerViewSettings ViewSettings { get; }

    private bool isModified = false;
    private bool canCutCopyDelete = false;
    private bool canPaste = false;
    private int selectedIndex = -1;

    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }
    public RelayCommand MarkAsGivenCommand { get; }
    public RelayCommand CutCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand PasteCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public PuzzleViewModel(Settings.PerViewSettings viewSettings)
    {
        Model = new PuzzleModel();
        Cells = new CellList();
        ViewSettings = viewSettings;

        UndoCommand = new RelayCommand(ExecuteUndo, CanUndo);
        RedoCommand = new RelayCommand(ExecuteRedo, CanRedo);

        CutCommand = new RelayCommand(ExecuteCut, p => canCutCopyDelete);
        CopyCommand = new RelayCommand(ExecuteCopy, p => canCutCopyDelete);
        PasteCommand = new RelayCommand(ExecutePaste, p => canPaste);
        DeleteCommand = new RelayCommand(ExecuteDelete, p => canCutCopyDelete);

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
                return Model.Add;

            if (currentValue != newValue)
            {
                if ((cell.Origin == Origins.User) || (cell.Origin == Origins.Given))
                    return Model.Edit;

                if ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated))
                    return Model.EditForced;
            }

            if ((currentValue == newValue) && ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated)))
                return Model.SetOrigin;
        }

        if ((newValue == 0) && (currentValue > 0) && ((cell.Origin == Origins.User) || (cell.Origin == Origins.Given)))
            return Model.Delete;

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
                IsModified = true;
            }
            else
            {
                Utils.PlayExclamation();
            }
        }
    }

    public void Save(Stream stream)
    {
        Model.Save(stream);
        IsModified = false;
    }

    public void Open(Stream stream)
    {
        try
        {
            Model.Clear();
            Model.Open(stream);
            IsModified = false;
        }
        catch
        {
            Model.Clear();
            throw;
        }
        finally
        {
            UpdateView();
        }
    }

    private void UpdateView()
    {
        // update the view model's observable collection, causing a ui update
        foreach (Models.Cell modelCell in Model.Cells)
        {
            int index = modelCell.Index;

            if (!modelCell.Equals(Cells[index]))
                Cells[index] = new Cell(modelCell);
        }

        Debug.Assert(Model.CompletedCellCountIsValid);
    }

    public void New()
    {
        Model.Clear();
        UpdateView();
        IsModified = false;
    }

    public bool ShowPossibles
    {
        get => ViewSettings.ShowPossibles;
        set
        {
            if (ViewSettings.ShowPossibles != value)
            {
                ViewSettings.ShowPossibles = value;
                Settings.Data.ViewSettings.ShowPossibles = value;
                UpdateViewForShowPossiblesStateChange();
                NotifyPropertyChanged();
            }
        }
    }
   
    public bool ShowSolution
    {
        get => ViewSettings.ShowSolution;
        set
        {
            if (ViewSettings.ShowSolution != value)
            {
                ViewSettings.ShowSolution = value;
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

            MarkAsGivenCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanUndo(object? param) => false;

    public void ExecuteUndo(object? param) {}


    public bool CanRedo(object? param) => false;

    public void ExecuteRedo(object? param) {}

    public void SelectedCellChanged(int index)
    {
        if (selectedIndex != index)
        {
            selectedIndex = index;

            Debug.WriteLine($"UpdateCellForSelection new selected: {index}");

            if (canCutCopyDelete != Cells[index].HasValue)
            {
                canCutCopyDelete = Cells[index].HasValue;

                CutCommand.RaiseCanExecuteChanged();
                CopyCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public async Task ClipboardContentChanged()
    {
        bool newValue = await ReadClipboardNumber() > 0;

        if (canPaste != newValue)
        {
            canPaste = newValue;
            PasteCommand.RaiseCanExecuteChanged();
        }
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
        ExecuteCopy(null);
        ExecuteDelete(null);
    }

    public void ExecuteCopy(object? param) 
    {
        if (selectedIndex >= 0)
        {
            DataPackage dp = new DataPackage();
            dp.SetText(Cells[selectedIndex].Value.ToString());
            Clipboard.SetContent(dp);
        }
    }

    public async void ExecutePaste(object? param) 
    {
        if (selectedIndex >= 0)
        {
            int newValue = await ReadClipboardNumber();

            if (newValue > 0)
                UpdateCellForKeyDown(selectedIndex, newValue);
        }
    }

    public void ExecuteDelete(object? param) 
    {
        if (selectedIndex >= 0)
            UpdateCellForKeyDown(selectedIndex, 0);
    }

    public bool CanMarkAsGiven(object? param) => Cells.Any(c => c.Origin == Origins.User);

    public void ExecuteMarkAsGiven(object? param) 
    {
        Model.SetOriginToGiven();
        UpdateView();
        IsModified = true;
    }


    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
