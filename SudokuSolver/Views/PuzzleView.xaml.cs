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

    public PuzzleView()
    {
        InitializeComponent();

        // if the app theme is different from the systems an initial opacity of zero stops  
        // excessive background flashing when creating new tabs, looks intentional...
        Loaded += PuzzleView_Loaded;

        static void PuzzleView_Loaded(object sender, RoutedEventArgs e)
        {
            PuzzleView puzzleView = (PuzzleView)sender;
            puzzleView.Grid.Opacity = 1;
            puzzleView.Loaded -= PuzzleView_Loaded;
        }
    }

    public PuzzleView(bool isPrintView) : this()
    {
        if (!isPrintView)
        {
            SizeChanged += (s, e) =>
            {
                // stop the grid lines being interpolated out when the view box scaling goes below 1.0
                // WPF did this automatically. Printers will typically have much higher DPI resolutions.
                Grid.AdaptForScaleFactor(e.NewSize.Width);
            };
        }
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

    public void FocusLastSelectedCell() => lastSelectedCell?.Focus(FocusState.Programmatic);
}
