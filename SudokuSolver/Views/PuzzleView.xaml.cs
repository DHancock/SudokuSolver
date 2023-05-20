using Microsoft.UI.Xaml.Media;

using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    private PuzzleViewModel? viewModel;
    private readonly Cell[] cells = new Cell[9 * 9];
    private int lastSelectedIndex = -1;

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

        Loaded += (s, e) =>
        {
            for (int index = 0; index < cells.Length; index++)
            {
                cells[index] = (Cell)Grid.Children[index];

                cells[index].SelectionChanged += (s, args) =>
                {
                    if (args.IsSelected) // enforce single selection
                    {
                        ViewModel?.SelectedCellChanged(args.CellIndex);

                        if ((lastSelectedIndex >= 0) && (lastSelectedIndex != args.CellIndex))
                            cells[lastSelectedIndex].IsSelected = false;

                        lastSelectedIndex = args.CellIndex;
                    }
                };
            }
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
