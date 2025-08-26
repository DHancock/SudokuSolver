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

    public PuzzleView()
    {
        InitializeComponent();

        Grid.Loaded += Grid_Loaded;
        Unloaded += PuzzleView_Unloaded;
        SizeChanged += PuzzleView_SizeChanged;
    }

    private static void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        // if the app theme is different from the systems an initial opacity of zero stops  
        // excessive background flashing when creating new tabs, looks intentional...
        SudokuGrid grid = (SudokuGrid)sender;
        grid.Opacity = 1;
    }

    private static void PuzzleView_Unloaded(object sender, RoutedEventArgs e)
    {
        PuzzleView puzzleView = (PuzzleView)sender;
        puzzleView.themeWhenSelected = puzzleView.ActualTheme;
    }

    private static void PuzzleView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // stop the grid lines being interpolated out when the view box scaling goes below 1.0
        // WPF did this automatically. Printers will typically have much higher DPI resolutions.
        PuzzleView puzzleView = (PuzzleView)sender;

        if (!puzzleView.IsPrintView)
        {
            puzzleView.Grid.AdaptForScaleFactor(e.NewSize.Width);
        }
    }

    public void Closed()
    {
        Grid.Loaded += Grid_Loaded;
        Unloaded += PuzzleView_Unloaded;
        SizeChanged += PuzzleView_SizeChanged;

        viewModel = null;
        lastSelectedCell = null;
    }

    public PuzzleViewModel ViewModel  
    {
        get
        {
            Debug.Assert(viewModel is not null);
            return viewModel;
        }
        set
        {
            Debug.Assert(value is not null);
            viewModel = value;
        }
    }

    public BrushTransition BackgroundBrushTransition => PuzzleBrushTransition;

    public void CellSelectionChanged(Cell cell, int index, bool isSelected)
    {
        ViewModel.SelectedIndexChanged(index, isSelected);

        if (isSelected)
        {
            // enforce single selection
            if (lastSelectedCell is not null)
            {
                lastSelectedCell.IsSelected = false;
            }

            lastSelectedCell = cell;
        }
        else if (ReferenceEquals(lastSelectedCell, cell))
        {
            lastSelectedCell = null;
        }
    }

    public void FocusLastSelectedCell()
    {
        if (lastSelectedCell is not null)
        {
            if (lastSelectedCell.Parent is not null)
            {
                bool success = lastSelectedCell.Focus(FocusState.Programmatic);
                Debug.Assert(success);
            }
            else
            {
                // When a TabSelectionChanged event is received the new tab's visual tree hasn't yet been restored
                // and the selected cells parent will be null. It still is when the parent TabViewItem gets focus.
                // That would cause an attempt to focus the selected cell to fail.
                Task.Run(() =>
                {
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        // because it's queued circumstances may have changed so there's little point asserting success
                        lastSelectedCell?.Focus(FocusState.Programmatic);
                    });
                });
            }
        }
    }

    public void ResetOpacityTransitionForThemeChange()
    {
        Debug.Assert(!IsLoaded);
        // the next time this tab is loaded the opacity transition may need to be restarted
        Grid.Opacity = (themeWhenSelected != Utils.NormaliseTheme(Settings.Instance.Theme)) ? 0 : 1;
    }
}
