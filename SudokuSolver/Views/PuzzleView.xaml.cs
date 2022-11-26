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
     
        Loaded += async (s, e) =>
        {
            // switch on the brush transition animation only for user initiated theme
            // changes, not when the window is opened to avoid ui flashing.
            await Task.Delay(250);
            ((PuzzleView)s).PuzzleBrushTransition.Duration = new TimeSpan(0, 0, 0, 0, 250);
        };
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
