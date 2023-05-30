using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    private PuzzleViewModel? viewModel;
    private Cell? lastSelectedCell;
    public event TypedEventHandler<PuzzleView, Cell.SelectionChangedEventArgs>? SelectedIndexChanged;

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

    private void Cell_SelectionChanged(Cell sender, Cell.SelectionChangedEventArgs e)
    {
        SelectedIndexChanged?.Invoke(this, e);

        if (e.IsSelected)
        {
            // enforce single selection
            if (lastSelectedCell is not null)
                lastSelectedCell.IsSelected = false;

            lastSelectedCell = sender;
        }
        else if (ReferenceEquals(lastSelectedCell, sender))
        {
            lastSelectedCell = null;
        }
    }

    public void FocusLastSelectedCell()
    {
        if (lastSelectedCell is not null)
        {
            bool success = lastSelectedCell.Focus(FocusState.Programmatic);
            Debug.Assert(success);
        }
    }
}
