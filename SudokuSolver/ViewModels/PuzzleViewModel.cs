using Sudoku.Utils;
using Sudoku.Common;
using Sudoku.Models;

namespace Sudoku.ViewModels;

internal sealed class PuzzleViewModel : INotifyPropertyChanged
{
    private Settings Settings { get; }
    private PuzzleModel Model { get; }
    public CellList Cells { get; }
    public RelayCommand ClearCommand { get; }

    public PuzzleViewModel(string settingsText)
    {
        Model = new PuzzleModel();
        Settings = DeserializeSettings(settingsText);
        Cells = new CellList();
        ClearCommand = new RelayCommand(ClearCommandHandler, o => !Model.PuzzleIsEmpty);
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
            }
            else
            {
                User32Sound.PlayExclamation();
            }
        }
    }

    public void Save(Stream stream) => Model.Save(stream);

    public void Open(Stream stream)
    {
        try
        {
            Model.Clear();
            Model.Open(stream);
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
        ClearCommand.RaiseCanExecuteChanged();
    }

    private void ClearCommandHandler(object? _)
    {
        Model.Clear();
        UpdateView();
    }

    public bool ShowPossibles
    {                                                        
        get => Settings.ShowPossibles;
        set
        {
            if (value != Settings.ShowPossibles)
            {
                Settings.ShowPossibles = value;
                UpdateViewForShowPossiblesStateChange();
                NotifyPropertyChanged();
            }
        }
    }
    
    public ElementTheme Theme
    {
        get => Settings.IsDarkThemed ? ElementTheme.Dark : ElementTheme.Light;
    }

    public bool IsDarkThemed
    {
        get => Settings.IsDarkThemed;
        set
        {
            if (value != Settings.IsDarkThemed)
            {
                Settings.IsDarkThemed = value;
                NotifyPropertyChanged(nameof(Theme));
                NotifyPropertyChanged();
            }
        }
    }

    public bool ShowSolution
    {
        get => Settings.ShowSolution;
        set
        {
            if (value != Settings.ShowSolution)
            {
                Settings.ShowSolution = value; 
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

    private static Settings DeserializeSettings(string data)
    {
        if (!string.IsNullOrWhiteSpace(data))
        {
            try
            {
                Settings? settings = JsonSerializer.Deserialize<Settings>(data, GetSerializerOptions());

                if (settings is not null)
                    return settings;
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
        }

        return new Settings();
    }

    public string SerializeSettings() => JsonSerializer.Serialize(Settings, GetSerializerOptions());

    public WINDOWPLACEMENT WindowPlacement
    {
        get => Settings.WindowPlacement;
        set => Settings.WindowPlacement = value;
    }

    private static JsonSerializerOptions GetSerializerOptions()
    {
        return new JsonSerializerOptions()
        {
            WriteIndented = true,
            IncludeFields = true,
        };
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
