using SudokuSolver.Common;
using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for Cell.xaml
/// </summary>
internal sealed partial class Cell : UserControl
{
    private bool isFocused = false;
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
                GetParentPuzzleView().CellSelectionChanged(this, Data.Index, value);
            }

            if (isSelected && !isFocused)
            {
                bool success = Focus(FocusState.Programmatic);
                Debug.Assert(success);
            }

            AdjustCellVisualState();

            PuzzleView GetParentPuzzleView()
            {
                Debug.Assert(IsLoaded);
                return (PuzzleView)((Viewbox)((SudokuGrid)Parent).Parent).Parent;
            }
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        PointerPoint pointerInfo = e.GetCurrentPoint(this);

        if (pointerInfo.Properties.IsLeftButtonPressed)
        {
            if (IsSelected)
            {
                if (isFocused)
                {
                    IsSelected = false;
                }
                else
                {
                    bool success = Focus(FocusState.Programmatic);
                    Debug.Assert(success);
                }
            }
            else
            {
                IsSelected = true;
            }
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        isFocused = false;
        AdjustCellVisualState();
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
        isFocused = true;

        if (!IsSelected)
        {
            // User tabbed to this cell, or the window has been switched to the foreground.
            // Select because there is no current visual indication of a focused but unselected cell.
            IsSelected = true;
        }
        else
        {
            AdjustCellVisualState();
        }
    }

    private void AdjustCellVisualState()
    {
        if (isSelected)
        {
            if (isFocused)
            {
                GoToVisualState("SelectedFocused");
            }
            else
            {
                GoToVisualState("SelectedUnfocused");
            }
        }
        else
        {
            GoToVisualState("Normal");
        }
    }

    private void GoToVisualState(string state)
    {
        bool stateFound = VisualStateManager.GoToState(this, state, false);
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
            Origins origin = (vmCell.Origin == Origins.Trial) ? Origins.Calculated : vmCell.Origin;
#endif
            cell.GoToVisualState(origin.ToString());
            cell.CollapseAllPossibles();

            if (vmCell.ViewModel.ShowSolution || (vmCell.Origin == Origins.User) || (vmCell.Origin == Origins.Provided))
            {
                cell.CellValue.Opacity = 1;
                cell.CellValue.Text = vmCell.Value.ToString();
            }
            else
            {
                cell.CellValue.Opacity = 0;  // still allows hit testing
            }
        }
        else
        {
            cell.CellValue.Opacity = 0;

            if (!vmCell.ViewModel.ShowPossibles)
            {
                cell.CollapseAllPossibles();
            }
            else
            {
                cell.UpdatePossible(cell.PossibleValue1, 1, vmCell);
                cell.UpdatePossible(cell.PossibleValue2, 2, vmCell);
                cell.UpdatePossible(cell.PossibleValue3, 3, vmCell);
                cell.UpdatePossible(cell.PossibleValue4, 4, vmCell);
                cell.UpdatePossible(cell.PossibleValue5, 5, vmCell);
                cell.UpdatePossible(cell.PossibleValue6, 6, vmCell);
                cell.UpdatePossible(cell.PossibleValue7, 7, vmCell);
                cell.UpdatePossible(cell.PossibleValue8, 8, vmCell);
                cell.UpdatePossible(cell.PossibleValue9, 9, vmCell);
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
            e.Handled = true;
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
                Data.ViewModel.UpdateCellForKeyDown(Data.Index, newValue);
                e.Handled = true;
            }
        }
    }

    private void CollapseAllPossibles()
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

    private void UpdatePossible(TextBlock tb, int index, ViewModels.Cell vmCell)
    {
        if (vmCell.Possibles[index])
        {
            if (vmCell.VerticalDirections[index])
            {
                GoToVisualState(string.Concat("Vertical", tb.Text));
            }
            else if (vmCell.HorizontalDirections[index])
            {
                GoToVisualState(string.Concat("Horizontal", tb.Text));
            }
            else
            {
                GoToVisualState(string.Concat("Normal", tb.Text));
            }

            tb.Visibility = Visibility.Visible;
        }
        else
        {
            tb.Visibility = Visibility.Collapsed;
        }
    }

    private void MenuFlyout_Opening(object sender, object e)
    {
        Debug.Assert(ContextFlyout is MenuFlyout);
        Debug.Assert(((MenuFlyout)ContextFlyout).Items.Count == 3);

        ViewModels.Cell vmCell = Data;
        MenuFlyout menu = (MenuFlyout)ContextFlyout;

        menu.OverlayInputPassThroughElement ??= App.Instance.GetWindowForElement(this)?.Content;

        // cut, copy, paste
        menu.Items[0].IsEnabled = PuzzleViewModel.CanCut(vmCell);
        menu.Items[1].IsEnabled = PuzzleViewModel.CanCopy(vmCell);
        menu.Items[2].IsEnabled = PuzzleViewModel.CanPaste(vmCell);
    }

    private void MenuFlyoutItem_Cut(object sender, RoutedEventArgs e)
    {
        ViewModels.Cell vmCell = Data;

        if (PuzzleViewModel.CanCut(vmCell))
        {
            ClipboardHelper.Copy(vmCell.Value);
            vmCell.ViewModel.UpdateCellForKeyDown(vmCell.Index, 0); // delete
        }
    }

    private void MenuFlyoutItem_Copy(object sender, RoutedEventArgs e)
    {
        ViewModels.Cell vmCell = Data;

        if (PuzzleViewModel.CanCopy(vmCell))
        {
            ClipboardHelper.Copy(vmCell.Value);
        }
    }
    
    private void MenuFlyoutItem_Paste(object sender, RoutedEventArgs e)
    {
        ViewModels.Cell vmCell = Data;

        if (PuzzleViewModel.CanPaste(vmCell))
        {
            vmCell.ViewModel.UpdateCellForKeyDown(vmCell.Index, App.Instance.ClipboardHelper.Value);

            if (!IsSelected && isFocused)
            {
                // there is no current visual indication of a focused but unselected cell
                IsSelected = true;
            }
        }
    }
}