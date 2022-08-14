using Sudoku.Common;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for Cell.xaml
/// </summary>
internal sealed partial class Cell : UserControl
{
    private static readonly string[] sLookUp = new[] { string.Empty, "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    private readonly PossibleTextBlock[] possibleTBs;
    
    public Cell()
    {
        this.InitializeComponent();

        IsTabStop = true;
        IsHitTestVisible = true;

        LosingFocus += Cell_LosingFocus;

        possibleTBs = new PossibleTextBlock[9] { PossibleValue0, PossibleValue1, PossibleValue2, PossibleValue3, PossibleValue4, PossibleValue5, PossibleValue6, PossibleValue7, PossibleValue8 };
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        bool focused = Focus(FocusState.Programmatic);
        Debug.Assert(focused);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    { 
        bool stateFound = VisualStateManager.GoToState(this, "Unfocused", false);
        Debug.Assert(stateFound);
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
        bool stateFound = VisualStateManager.GoToState(this, "Focused", false);
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
        ViewModels.Cell data = (ViewModels.Cell)e.NewValue;

        if (data.HasValue)
        {
            // sets the colour of the cell value text, be it user entered, calculated or trial and error.
            bool stateFound = VisualStateManager.GoToState(cell, data.Origin.ToString(), false);
            Debug.Assert(stateFound); 

            if (((ViewModels.PuzzleViewModel)cell.DataContext).ShowSolution || (data.Origin == Origins.User))
            {
                cell.CellValue.Text = sLookUp[data.Value];
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
                    if (data.Possibles[i])
                    {
                        PossibleTextBlock tb = cell.possibleTBs[writeIndex++];
                        tb.Text = sLookUp[i];

                        bool stateFound;

                        if (data.VerticalDirections[i])
                            stateFound = VisualStateManager.GoToState(tb, "Vertical", false);
                        else if (data.HorizontalDirections[i])
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
        Cell? nextCell = null;
        int newValue = -1;

        switch (e.Key)
        {
            case VirtualKey.Left:   nextCell = (Cell)XYFocusLeft; break;
            case VirtualKey.Right:  nextCell = (Cell)XYFocusRight; break;
            case VirtualKey.Up:     nextCell = (Cell)XYFocusUp; break;
            case VirtualKey.Down:   nextCell = (Cell)XYFocusDown; break;

            case > VirtualKey.Number0 and <= VirtualKey.Number9:
                newValue = e.Key - VirtualKey.Number0;
                break;

            case > VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9:
                newValue = e.Key - VirtualKey.NumberPad0;
                break;

            case VirtualKey.Delete: newValue = 0; break;
            case VirtualKey.Back:   newValue = 0; break;

            default:break;
        }

        if (nextCell is not null)
        {
            e.Handled = true;
            bool focused = nextCell.Focus(FocusState.Programmatic);
            Debug.Assert(focused);
        }
        else if (newValue >= 0)
        {
            e.Handled = true;
            ((ViewModels.PuzzleViewModel)this.DataContext).UpdateCellForKeyDown(this.Data.Index, newValue);
        }
    }
}
