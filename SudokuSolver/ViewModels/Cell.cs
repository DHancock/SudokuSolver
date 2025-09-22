using SudokuSolver.Common;

namespace SudokuSolver.ViewModels;

internal sealed class Cell : CellBase
{

    public PuzzleViewModel ViewModel { get; }

    public Cell(int index, PuzzleViewModel viewModel) : base(index) // default value
    {
        ViewModel = viewModel;
    }

    public Cell(Cell source) : base(source) // a view model property has changed
    {
        ViewModel = source.ViewModel;
    }

    public Cell(Models.Cell source, PuzzleViewModel viewModel) : base(source)  // a new model value has been calculated
    {
        ViewModel = viewModel;
    }
}
