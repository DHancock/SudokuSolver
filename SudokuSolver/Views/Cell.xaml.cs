using SudokuSolver.Common;
using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for Cell.xaml
/// </summary>
internal sealed partial class Cell : UserControl
{
    private bool isFocused = false;
    private bool isSelected = false;

    private int version = int.MinValue;
    private ViewModels.Cell? viewModelCell;

    public Cell()
    {
        this.InitializeComponent();

        IsTabStop = true;
        IsHitTestVisible = true;
        LosingFocus += Cell_LosingFocus;
        GettingFocus += Cell_GettingFocus;
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected != value)
            {
                isSelected = value;
                GetParentPuzzleView().CellSelectionChanged(this, ViewModelCell.Index, value);
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

            e.Handled = true;
        }
    }

    private void Cell_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        bool cancelled = false;

        if ((args.NewFocusedElement is TabViewItem) || (args.NewFocusedElement is ScrollViewer))
        {
            cancelled = args.TryCancel();
            Debug.Assert(cancelled);
        }

        if (!cancelled)
        {
            isFocused = false;
            args.Handled = true;

            AdjustCellVisualState();
        }
    }

    private void Cell_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        isFocused = true;

        if (!IsSelected && !((args.OldFocusedElement is Popup) || (args.OldFocusedElement is MenuBarItem)))
        {
            // Unless a menu has just closed, set as selected. It's the default state for a focused cell
            IsSelected = true;
        }

        AdjustCellVisualState();
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

    public ViewModels.Cell ViewModelCell
    {
        get
        {
            Debug.Assert(viewModelCell is not null);
            return viewModelCell;
        }
        set
        {
            Debug.Assert(value is not null);

            if (version != value.Version)
            {
                viewModelCell = value;
                version = value.Version;
                ViewModelCellChangedCallback();
            }
        }
    }

    private void GoToVisualState(string state)
    {
        bool stateFound = VisualStateManager.GoToState(this, state, false);
        Debug.Assert(stateFound);
    }

    private void ViewModelCellChangedCallback()
    {
        Debug.Assert(viewModelCell is not null);

        if (viewModelCell.HasValue)
        {
            GoToVisualState(viewModelCell.Origin.ToString());
            CollapseAllPossibles();

            if (viewModelCell.ViewModel.ShowSolution || (viewModelCell.Origin == Origins.User) || (viewModelCell.Origin == Origins.Provided))
            {
                CellValue.Opacity = 1;
                CellValue.Text = viewModelCell.Value.ToString();
            }
            else
            {
                CellValue.Opacity = 0;  // still allows hit testing
            }
        }
        else
        {
            CellValue.Opacity = 0;

            if (!viewModelCell.ViewModel.ShowPossibles)
            {
                CollapseAllPossibles();
            }
            else
            {
                UpdatePossible(PossibleValue1, 1);
                UpdatePossible(PossibleValue2, 2);
                UpdatePossible(PossibleValue3, 3);
                UpdatePossible(PossibleValue4, 4);
                UpdatePossible(PossibleValue5, 5);
                UpdatePossible(PossibleValue6, 6);
                UpdatePossible(PossibleValue7, 7);
                UpdatePossible(PossibleValue8, 8);
                UpdatePossible(PossibleValue9, 9);
            }
        }

        void CollapseAllPossibles()
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

        void UpdatePossible(TextBlock tb, int index)
        {
            if (viewModelCell.Possibles[index])
            {
                if (viewModelCell.VerticalDirections[index])
                {
                    GoToVisualState(string.Concat("Vertical", tb.Text));
                }
                else if (viewModelCell.HorizontalDirections[index])
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
    }

    // The new value for the cell is forwarded by the view model to the model 
    // which recalculates the puzzle. After that each model cell is compared to
    // the view models cell and updated if different. That updates the ui as
    // the view model cell list is an observable collection bound to ui cells.
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        Debug.Assert(viewModelCell is not null);

        int newIndex = -1; 

        if (e.Key == VirtualKey.Up)
        {
            newIndex = Utils.Clamp2DVerticalIndex(viewModelCell.Index - SudokuGrid.cCellsInRow, SudokuGrid.cCellsInRow, SudokuGrid.cCellCount);
        }
        else if (e.Key == VirtualKey.Down)
        {
            newIndex = Utils.Clamp2DVerticalIndex(viewModelCell.Index + SudokuGrid.cCellsInRow, SudokuGrid.cCellsInRow, SudokuGrid.cCellCount);
        }
        else if (e.Key == VirtualKey.Left)
        {
            newIndex = Utils.Clamp2DHorizontalIndex(viewModelCell.Index - 1, SudokuGrid.cCellCount);
        }
        else if (e.Key == VirtualKey.Right)
        {
            newIndex = Utils.Clamp2DHorizontalIndex(viewModelCell.Index + 1, SudokuGrid.cCellCount);
        }

        if (newIndex >= 0)
        {
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
                viewModelCell.ViewModel.UpdateCellForKeyDown(viewModelCell.Index, newValue);
                e.Handled = true;
            }
        }
    }
}