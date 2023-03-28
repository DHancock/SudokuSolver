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

    public PuzzleViewModel(Settings.PerViewSettings viewSettings)
    {
        Model = new PuzzleModel();
        Cells = new CellList();
        ViewSettings = viewSettings;
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
                if (currentOrigin == Origins.User)
                    return Model.Edit;

                if ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated))
                    return Model.EditForced;
            }

            if ((currentValue == newValue) && ((currentOrigin == Origins.Trial) || (currentOrigin == Origins.Calculated)))
                return Model.SetOrigin;
        }

        if ((newValue == 0) && (currentValue > 0) && (currentOrigin == Origins.User))
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
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
