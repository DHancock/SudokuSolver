using Microsoft.UI.Xaml.Data;

using SudokuSolver.Common;
using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for Cell.xaml
/// </summary>
internal sealed partial class Cell : UserControl
{
    private static readonly string[] sLookUp = new[] { string.Empty, "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    private enum VisualState { None, SelectedFocused, SelectedUnfocused }
    private VisualState currentVisualState = VisualState.None;

    private readonly PossibleTextBlock[] possibleTBs;

    public event TypedEventHandler<Cell, SelectionChangedEventArgs>? SelectionChanged;

    private bool isSelected = false;
    
    public Cell()
    {
        this.InitializeComponent();

        IsTabStop = true;
        IsHitTestVisible = true;
        LosingFocus += Cell_LosingFocus;

        possibleTBs = new PossibleTextBlock[9] { PossibleValue0, PossibleValue1, PossibleValue2, PossibleValue3, PossibleValue4, PossibleValue5, PossibleValue6, PossibleValue7, PossibleValue8 };
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected != value)
            {
                isSelected = value;

                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(Data.Index, value));

                if (!isSelected) // set by the puzzle to enforce single selection
                    GoToVisualState(VisualState.None);
            }
        }
    }

    internal record SelectionChangedEventArgs(int Index, bool IsSelected);

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        IsSelected = !IsSelected;

        if (IsSelected)
        {
            bool success = Focus(FocusState.Programmatic);
            Debug.Assert(success);

            if (success)
                GoToVisualState(VisualState.SelectedFocused);
        }
        else
            GoToVisualState(VisualState.None);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (IsSelected)
            GoToVisualState(VisualState.SelectedUnfocused);
        else
            GoToVisualState(VisualState.None);
    }

    // Above every thing else in the visual tree is a scroll viewer that's
    // provided by Microsoft. It steals focus immediately after a User
    // Control receives focus. This cancels that focus change.
    private void Cell_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        if (args.NewFocusedElement is ScrollViewer)
        {
            bool cancelled = args.TryCancel();
            Debug.Assert(cancelled);
        }
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        if (!IsSelected) 
            IsSelected = true; // user tabbed to cell, or window switched to foreground

        GoToVisualState(VisualState.SelectedFocused);
    }

    private void GoToVisualState(VisualState state)
    {
        if (state != currentVisualState)
        {
            bool stateFound = VisualStateManager.GoToState(this, state.ToString(), false);
            Debug.Assert(stateFound, $"unknown visual state: {state.ToString()}");
            currentVisualState = state;
        }
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
        ViewModels.Cell viewModelCell = (ViewModels.Cell)e.NewValue;

        if (viewModelCell.HasValue)
        {
            // sets the cell value text and color
#if DEBUG
            bool stateFound = VisualStateManager.GoToState(cell, viewModelCell.Origin.ToString(), false);
#else
            Origins origin = (viewModelCell.Origin == Origins.Trial) ? Origins.Calculated : viewModelCell.Origin;
            bool stateFound = VisualStateManager.GoToState(cell, origin.ToString(), false);
#endif
            Debug.Assert(stateFound); 

            if (((ViewModels.PuzzleViewModel)cell.DataContext).ShowSolution || (viewModelCell.Origin == Origins.User) || (viewModelCell.Origin == Origins.Provided))
            {
                cell.CellValue.Text = sLookUp[viewModelCell.Value];
            }
            else
            {
                cell.CellValue.Text = string.Empty;
            }

            foreach (PossibleTextBlock tb in cell.possibleTBs)
                tb.Text = string.Empty;
        }
        else
        {
            cell.CellValue.Text = string.Empty;

            if (((ViewModels.PuzzleViewModel)cell.DataContext).ShowPossibles)
            {
                int writeIndex = 0;

                for (int i = 1; i < 10; i++)
                {
                    if (viewModelCell.Possibles[i])
                    {
                        PossibleTextBlock tb = cell.possibleTBs[writeIndex++];
                        tb.Text = sLookUp[i];

                        bool stateFound;

                        if (viewModelCell.VerticalDirections[i])
                            stateFound = VisualStateManager.GoToState(tb, "Vertical", false);
                        else if (viewModelCell.HorizontalDirections[i])
                            stateFound = VisualStateManager.GoToState(tb, "Horizontal", false);
                        else
                            stateFound = VisualStateManager.GoToState(tb, "None", false);

                        Debug.Assert(stateFound);
                    }
                }

                while (writeIndex < 9)
                    cell.possibleTBs[writeIndex++].Text = string.Empty;
            }
            else
            {
                foreach (PossibleTextBlock tb in cell.possibleTBs)
                    tb.Text = string.Empty;
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
                ((ViewModels.PuzzleViewModel)this.DataContext).UpdateCellForKeyDown(Data.Index, newValue);
        }
    }
}
