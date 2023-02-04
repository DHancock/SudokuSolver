using Microsoft.UI.Xaml.Media;

using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    private PuzzleViewModel? viewModel;
    public bool IsPrintView { get; set; } = false;

    public PuzzleView()
    {
        InitializeComponent();

        SizeChanged += (s, e) =>
        {
            // printers generally have much higher resolutions than monitors
            if (!IsPrintView)
                Grid.AdaptForScaleFactor(e.NewSize.Width);
        };
    }
     
    public PuzzleViewModel? ViewModel
    {
        get => viewModel;

        set
        {
            Debug.Assert(value is not null);
            DataContext = viewModel = value;
        }
    }

    public BrushTransition BackgroundBrushTransition => PuzzleBrushTransition;
}
