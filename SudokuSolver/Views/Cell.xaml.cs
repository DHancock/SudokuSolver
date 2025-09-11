using SudokuSolver.Common;
using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for Cell.xaml
/// </summary>
internal sealed partial class Cell : UserControl
{
    private static readonly string[] sLookUp = [string.Empty, "1", "2", "3", "4", "5", "6", "7", "8", "9"];
    private enum VisualState { Normal, SelectedFocused, SelectedUnfocused, PointerOver }

    private bool isSelected = false;

    public Cell()
    {
        this.InitializeComponent();

        IsTabStop = true;
        IsHitTestVisible = true;
        LosingFocus += Cell_LosingFocus;
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected != value)  
            {
                isSelected = value;

                ParentPuzzleView.CellSelectionChanged(this, Data.Index, value);

                GoToVisualState(value ? VisualState.SelectedFocused : VisualState.Normal);
            }
        }
    }

    private PuzzleView ParentPuzzleView => (PuzzleView)((Viewbox)((SudokuGrid)this.Parent).Parent).Parent;

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        IsSelected = !IsSelected;

        if (IsSelected)
        {
            bool success = Focus(FocusState.Programmatic);
            Debug.Assert(success);

            if (success)
            {
                GoToVisualState(VisualState.SelectedFocused);
            }
        }
        else
        {
            GoToVisualState(VisualState.Normal);
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (IsSelected)
        {
            GoToVisualState(VisualState.SelectedUnfocused);
        }
        else
        {
            GoToVisualState(VisualState.Normal);
        }
    }

    private void Cell_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        if ((args.NewFocusedElement is TabViewItem) || (args.NewFocusedElement is ScrollViewer))
        {
            bool cancelled = args.TryCancel();
            Debug.Assert(cancelled);
        }
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        if (!IsSelected) 
        {
            IsSelected = true; // user tabbed to cell, or window switched to foreground
        }

        GoToVisualState(VisualState.SelectedFocused);
    }

    private void GoToVisualState(VisualState state)
    {
        bool stateFound = VisualStateManager.GoToState(this, state.ToString(), false);
        Debug.Assert(stateFound);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data),
            typeof(ViewModels.Cell),
            typeof(Cell),
            new PropertyMetadata(null, CellDataChangedCallback));

    public ViewModels.Cell Data
    {
        get { return (ViewModels.Cell)GetValue(DataProperty); }
        set { base.SetValue(DataProperty, value); }
    }

    private static void CellDataChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        Cell cell = (Cell)d;
        ViewModels.Cell vmCell = (ViewModels.Cell)e.NewValue;

        if (vmCell.HasValue)
        {
#if DEBUG
            Origins origin = vmCell.Origin;
#else
            Origins origin = (cellData.Origin == Origins.Trial) ? Origins.Calculated : cellData.Origin;
#endif
            bool stateFound = VisualStateManager.GoToState(cell, origin.ToString(), false);
            Debug.Assert(stateFound);

            cell.CollapseAllPossibleTextBlocks();

            if (cell.ParentPuzzleView.ViewModel.ShowSolution || (vmCell.Origin == Origins.User) || (vmCell.Origin == Origins.Provided))
            {
                cell.CellValue.Opacity = 1;
                cell.CellValue.Text = sLookUp[vmCell.Value];
            }
            else
            {
                cell.CellValue.Opacity = 0;  // allows for hit testing
            }
        }
        else
        {
            cell.CellValue.Opacity = 0;

            if (!cell.ParentPuzzleView.ViewModel.ShowPossibles)
            {
                cell.CollapseAllPossibleTextBlocks();
            }
            else
            {
                UpdatePossibleTextBlock(cell.PossibleValue1, 1, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue2, 2, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue3, 3, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue4, 4, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue5, 5, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue6, 6, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue7, 7, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue8, 8, vmCell);
                UpdatePossibleTextBlock(cell.PossibleValue9, 9, vmCell);
            }
        }
    }

    // The new value for the cell is forwarded by the view model to the model 
    // which recalculates the puzzle. After that each model cell is compared to
    // the view models cell and updated if different. That updates the ui as
    // the view model cell list is an observable collection bound to ui cells.
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if ((e.Key == VirtualKey.Up) || (e.Key == VirtualKey.Down) || (e.Key == VirtualKey.Left) || (e.Key == VirtualKey.Right))
        {
            int newIndex;

            if (e.Key == VirtualKey.Up)
            {
                newIndex = Utils.Clamp2DVerticalIndex(Data.Index - SudokuGrid.cCellsInRow, SudokuGrid.cCellsInRow, SudokuGrid.cCellCount);
            }
            else if (e.Key == VirtualKey.Down)
            {
                newIndex = Utils.Clamp2DVerticalIndex(Data.Index + SudokuGrid.cCellsInRow, SudokuGrid.cCellsInRow, SudokuGrid.cCellCount);
            }
            else if (e.Key == VirtualKey.Left)
            {
                newIndex = Utils.Clamp2DHorizontalIndex(Data.Index - 1, SudokuGrid.cCellCount);
            }
            else
            {
                newIndex = Utils.Clamp2DHorizontalIndex(Data.Index + 1, SudokuGrid.cCellCount);
            }

            ((SudokuGrid)Parent).Children[newIndex].Focus(FocusState.Programmatic);
        }
        else if (IsSelected)  // keyboard focus != selected
        {
            int newValue = -1;

            switch (e.Key)
            {
                case > VirtualKey.Number0 and <= VirtualKey.Number9:
                    newValue = e.Key - VirtualKey.Number0;
                    break;

                case > VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9:
                    newValue = e.Key - VirtualKey.NumberPad0;
                    break;

                case VirtualKey.Delete: newValue = 0; break;
                case VirtualKey.Back: newValue = 0; break;
                default: break;
            }

            if (newValue >= 0)
            {
                ParentPuzzleView.ViewModel.UpdateCellForKeyDown(Data.Index, newValue);
            }
        }
    }

    private void CollapseAllPossibleTextBlocks()
    {
        PossibleValue1.Visibility = Visibility.Collapsed;
        PossibleValue2.Visibility = Visibility.Collapsed;
        PossibleValue3.Visibility = Visibility.Collapsed;
        PossibleValue4.Visibility = Visibility.Collapsed;
        PossibleValue5.Visibility = Visibility.Collapsed;
        PossibleValue6.Visibility = Visibility.Collapsed;
        PossibleValue7.Visibility = Visibility.Collapsed;
        PossibleValue8.Visibility = Visibility.Collapsed;
        PossibleValue9.Visibility = Visibility.Collapsed;
    }

    private static void UpdatePossibleTextBlock(PossibleTextBlock ptb, int index, ViewModels.Cell vmCell)
    {
        if (vmCell.Possibles[index])
        {
            ptb.Visibility = Visibility.Visible;

            if (vmCell.VerticalDirections[index])
            {
                GoToVisualState(ptb, "Vertical");
            }
            else if (vmCell.HorizontalDirections[index])
            {
                GoToVisualState(ptb, "Horizontal");
            }
            else
            {
                GoToVisualState(ptb, "Normal");
            }
        }
        else
        {
            ptb.Visibility = Visibility.Collapsed;
        }

        static void GoToVisualState(Control control, string state)
        {
            bool stateFound = VisualStateManager.GoToState(control, state, false);
            Debug.Assert(stateFound);
        }
    }
}
