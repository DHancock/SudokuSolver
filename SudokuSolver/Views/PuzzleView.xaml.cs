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
        Grid.Loaded += (s, e) =>
        {
            Grid.Opacity = 1;
        };

        Unloaded += (s, e) =>
        {
            themeWhenSelected = ActualTheme;
        };

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

    public void FocusLastSelectedCell()
    {
        if (lastSelectedCell is not null)
        {
            bool focused = lastSelectedCell.Focus(FocusState.Programmatic);
            Debug.WriteLine($"FocusLastSelectedCell success = {focused}");
        }
    }

    public void ClearCellSelection()
    {
        if (lastSelectedCell is not null)
        {
            // attempting to focus the last selected cell when switching to this tab
            // fails probably due to the framework element's parent being null when the 
            // tab selection changed event is received. Just clear the selection now 
            // when switching from this tab to avoid leaving it in a invalid state. 
            lastSelectedCell.IsSelected = false;
            lastSelectedCell = null;
        }
    }

    public void ResetOpacityTransitionForThemeChange()
    {
        Debug.Assert(!IsLoaded);
        // the next time this tab is loaded the opacity transition may need to be restarted
        Grid.Opacity = (themeWhenSelected != Utils.NormaliseTheme(Settings.Instance.Theme)) ? 0 : 1;
    }
}
