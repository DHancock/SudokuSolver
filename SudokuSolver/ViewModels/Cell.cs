using SudokuSolver.Common;

namespace SudokuSolver.ViewModels;

internal sealed class Cell : CellBase
{
    public int Version { get; set; }
    public PuzzleViewModel ViewModel { get; }

    public Cell(int index, PuzzleViewModel viewModel) : base(index)
    {
        Version = 0;
        ViewModel = viewModel;
    }
}
