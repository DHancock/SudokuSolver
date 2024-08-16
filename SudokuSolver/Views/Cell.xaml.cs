using Microsoft.UI.Xaml.Data;

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

    private readonly TextBlock[] possibleTBs;

    public event TypedEventHandler<Cell, SelectionChangedEventArgs>? SelectionChanged;

    private bool isSelected = false;

    public Cell()
    {
        this.InitializeComponent();

        IsTabStop = true;
        IsHitTestVisible = true;
        LosingFocus += Cell_LosingFocus;

        possibleTBs = [PossibleValue0, PossibleValue1, PossibleValue2, PossibleValue3, PossibleValue4, PossibleValue5, PossibleValue6, PossibleValue7, PossibleValue8];
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

                GoToVisualState(value ? VisualState.SelectedFocused : VisualState.Normal);
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
        ViewModels.Cell cellData = (ViewModels.Cell)e.NewValue;

        if (cellData.HasValue)
        {
#if DEBUG
            Origins origin = cellData.Origin;
#else
            Origins origin = (cellData.Origin == Origins.Trial) ? Origins.Calculated : cellData.Origin;
#endif
            bool stateFound = VisualStateManager.GoToState(cell, origin.ToString(), false);
            Debug.Assert(stateFound);

            if (((ViewModels.PuzzleViewModel)cell.DataContext).ShowSolution || (cellData.Origin == Origins.User) || (cellData.Origin == Origins.Provided))
            {
                cell.CellValue.Opacity = 1;
                cell.CellValue.Text = sLookUp[cellData.Value];
            }
            else
            {
                cell.CellValue.Opacity = 0;  // allows for hit testing
            }

            if (cell.PossibleValue0.Visibility != Visibility.Collapsed)
            {
                foreach (TextBlock tb in cell.possibleTBs)
                {
                    tb.Visibility = Visibility.Collapsed;
                }
            }
        }
        else
        {
            SolidColorBrush? verticalBrush = null;
            SolidColorBrush? horizontalBrush = null;
            SolidColorBrush? normalBrush = null;

            cell.CellValue.Opacity = 0;

            if (((ViewModels.PuzzleViewModel)cell.DataContext).ShowPossibles)
            {
                int writeIndex = 0;

                for (int i = 1; i < 10; i++)
                {
                    if (cellData.Possibles[i])
                    {
                        TextBlock tb = cell.possibleTBs[writeIndex++];
                        tb.Visibility = Visibility.Visible;
                        tb.Text = sLookUp[i];

                        if (cellData.VerticalDirections[i])
                        {
                            verticalBrush ??= GetBrush(tb.ActualTheme, "PossiblesVerticalBrush");
                            tb.Foreground = verticalBrush;
                        }
                        else if (cellData.HorizontalDirections[i])
                        {
                            horizontalBrush ??= GetBrush(tb.ActualTheme, "PossiblesHorizontalBrush");
                            tb.Foreground = horizontalBrush;
                        }
                        else
                        {
                            normalBrush ??= GetBrush(tb.ActualTheme, "CellPossiblesBrush");
                            tb.Foreground = normalBrush;
                        }
                    }
                }

                while (writeIndex < 9)
                {
                    cell.possibleTBs[writeIndex++].Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                foreach (TextBlock tb in cell.possibleTBs)
                {
                    tb.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    private static SolidColorBrush GetBrush(ElementTheme theme, string key)
    {
        ResourceDictionary? rd = Utils.GetThemeDictionary(theme.ToString());

        if (rd is not null)
        {
            Debug.Assert(rd.ContainsKey(key));
            Debug.Assert(rd[key] is SolidColorBrush);

            return (SolidColorBrush)rd[key];
        }

        return new SolidColorBrush(Colors.Black);
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
                ((ViewModels.PuzzleViewModel)this.DataContext).UpdateCellForKeyDown(Data.Index, newValue);
            }
        }
    }
}
