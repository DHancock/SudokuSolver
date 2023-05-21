using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    private PuzzleViewModel? viewModel;
    private Cell? lastSelectedCell;
    public event EventHandler<int>? SelectedIndexChanged;

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

    private void Cell_SelectionChanged(object sender, Cell.SelectionChangedEventArgs e)
    {
        if (e.IsSelected) 
        {
            SelectedIndexChanged?.Invoke(this, e.CellIndex);

            // enforce single selection
            if (lastSelectedCell is not null)
                lastSelectedCell.IsSelected = false;

            lastSelectedCell = (Cell)sender;
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
