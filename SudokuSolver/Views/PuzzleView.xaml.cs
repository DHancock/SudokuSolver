using Sudoku.ViewModels;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    PuzzleViewModel? viewModel;

    public PuzzleView()
    {
        InitializeComponent();
    }

    public PuzzleViewModel? ViewModel
    {
        get => viewModel;

        set
        {
            viewModel = value;
            DataContext = value;
        }
    }
}
