using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    public bool IsPrintView { set; get; } = false;

    private ElementTheme themeWhenSelected;
    private PuzzleViewModel? viewModel;
    private Cell? lastSelectedCell;
    public event TypedEventHandler<PuzzleView, Cell.SelectionChangedEventArgs>? SelectedIndexChanged;

    public PuzzleView()
    {
        InitializeComponent();

        // if the app theme is different from the systems an initial opacity of zero stops  
        // excessive background flashing when creating new tabs, looks intentional...
        Loaded += PuzzleView_Loaded;
        Unloaded += PuzzleView_Unloaded;

        static void PuzzleView_Loaded(object sender, RoutedEventArgs e)
        {
            PuzzleView puzzleView = (PuzzleView)sender;
            puzzleView.Grid.Opacity = 1;
        }

        SizeChanged += (s, e) =>
        {
            // stop the grid lines being interpolated out when the view box scaling goes below 1.0
            // WPF did this automatically. Printers will typically have much higher DPI resolutions.
            if (!IsPrintView)
            {
                Grid.AdaptForScaleFactor(e.NewSize.Width);
            }
        };
    }

    private void PuzzleView_Unloaded(object sender, RoutedEventArgs e)
    {
        themeWhenSelected = Settings.Data.Theme;
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
            {
                lastSelectedCell.IsSelected = false;
            }

            lastSelectedCell = sender;
        }
        else if (ReferenceEquals(lastSelectedCell, sender))
        {
            lastSelectedCell = null;
        }
    }

    public void FocusLastSelectedCell() => lastSelectedCell?.Focus(FocusState.Programmatic);


    public void ResetOpacityTransitionForThemeChange()
    {
        // for existing tabs last selected when there was different theme, re-enable the opacity
        // transition to avoid more ui flashing
        Grid.Opacity = (Utils.NormaliseTheme(themeWhenSelected) != Utils.NormaliseTheme(Settings.Data.Theme)) ? 0 : 1;
    }
}
