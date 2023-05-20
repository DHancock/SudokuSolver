using SudokuSolver.Common;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for Cell.xaml
/// </summary>
internal sealed partial class Cell : UserControl
{
    private static readonly string[] sLookUp = new[] { string.Empty, "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    private readonly PossibleTextBlock[] possibleTBs;

    public event EventHandler<SelectedCellChangedEventArgs>? SelectionChanged;

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

                SelectionChanged?.Invoke(this, new SelectedCellChangedEventArgs(Data.Index, value));
               
                if (!isSelected)
                {
                    bool stateFound = VisualStateManager.GoToState(this, "None", false);
                    Debug.Assert(stateFound);
                }
            }
        }
    }

    internal record SelectedCellChangedEventArgs(int CellIndex, bool IsSelected);

    private bool IsFocused => FocusState != FocusState.Unfocused;

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        bool focused = Focus(FocusState.Programmatic);
        Debug.Assert(focused);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        Debug.Assert(IsSelected, "lost focus on an unselected cell");

        if (IsSelected)
        {
            bool stateFound = VisualStateManager.GoToState(this, "SelectedUnfocused", false);
            Debug.Assert(stateFound);
        }
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
        IsSelected = true;

        bool stateFound = VisualStateManager.GoToState(this, "SelectedFocused", false);
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
        ViewModels.Cell viewModelCell = (ViewModels.Cell)e.NewValue;

        if (viewModelCell.HasValue)
        {
            // sets the colour of the cell value text, be it user entered, calculated or trial and error.
            bool stateFound = VisualStateManager.GoToState(cell, viewModelCell.Origin.ToString(), false);
            Debug.Assert(stateFound); 

            if (((ViewModels.PuzzleViewModel)cell.DataContext).ShowSolution || (viewModelCell.Origin == Origins.User) || (viewModelCell.Origin == Origins.Given))
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
            ((ViewModels.PuzzleViewModel)this.DataContext).UpdateCellForKeyDown(Data.Index, newValue);
        }
    }
}
