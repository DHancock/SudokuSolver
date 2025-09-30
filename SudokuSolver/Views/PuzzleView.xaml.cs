using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    public bool IsPrintView { set; private get; } = false;

    private ElementTheme themeWhenSelected;
    private PuzzleViewModel? viewModel;
    private Cell? selectedCell;

    public PuzzleView()
    {
        InitializeComponent();

        Grid.Loaded += Grid_Loaded;
        Unloaded += PuzzleView_Unloaded;
        SizeChanged += PuzzleView_SizeChanged;
        PointerPressed += PuzzleView_PointerPressed;
    }

    private void PuzzleView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint pointerInfo = e.GetCurrentPoint(this);

        if (pointerInfo.Properties.IsRightButtonPressed)
        {
            Point offset = Utils.GetOffsetFromXamlRoot(this);

            offset.X += pointerInfo.Position.X;
            offset.Y += pointerInfo.Position.Y;

            foreach (UIElement element in VisualTreeHelper.FindElementsInHostCoordinates(offset, this))
            {
                if (element is Cell cell)
                {
                    ShowCellContextMenu(viaKeyboard: false, cell, offset);
                    e.Handled = true;
                    break;
                }
            }
        }
    }

    public bool ShowCellContextMenu()
    {
        if (selectedCell is not null)
        {
            ShowCellContextMenu(viaKeyboard: true, selectedCell, default);
            return true;
        }

        return false;
    }

    private void ShowCellContextMenu(bool viaKeyboard, Cell cell, Point offset)
    {
        // Use a custom cell context menu. If the cell itself had a context menu it would be
        // shown for the focused cell not necessarily the selected one on keyboard accelerator
        // activation. Hence a paste or cut could be made to a apparently random cell because  
        // there isn't any indication of a focused but not selected call.

        MenuFlyout menu = (MenuFlyout)Resources["CellContextMenu"];

        ViewModels.Cell vmCell = cell.Data;

        // cut, copy, paste
        menu.Items[0].Tag = vmCell;
        menu.Items[1].Tag = vmCell;
        menu.Items[2].Tag = vmCell;

        menu.Items[0].IsEnabled = PuzzleViewModel.CanCut(vmCell);
        menu.Items[1].IsEnabled = PuzzleViewModel.CanCopy(vmCell);
        menu.Items[2].IsEnabled = PuzzleViewModel.CanPaste();

        if (viaKeyboard) 
        {
            menu.ShowAt(cell);
        }
        else
        {
            menu.ShowAt(null, offset);
        }
    }

    private void MenuFlyoutItem_Cut(object sender, RoutedEventArgs e)
    {
        ViewModels.Cell vmCell = (ViewModels.Cell)((FrameworkElement)sender).Tag;

        if (PuzzleViewModel.CanCut(vmCell))
        {
            App.Instance.ClipboardHelper.Copy(vmCell.Value);
            vmCell.ViewModel.UpdateCellForKeyDown(vmCell.Index, 0); // delete
        }
    }

    private void MenuFlyoutItem_Copy(object sender, RoutedEventArgs e)
    {
        ViewModels.Cell vmCell = (ViewModels.Cell)((FrameworkElement)sender).Tag;

        if (PuzzleViewModel.CanCopy(vmCell))
        {
            App.Instance.ClipboardHelper.Copy(vmCell.Value);
        }
    }

    private void MenuFlyoutItem_Paste(object sender, RoutedEventArgs e)
    {
        ViewModels.Cell vmCell = (ViewModels.Cell)((FrameworkElement)sender).Tag;

        if (PuzzleViewModel.CanPaste())
        {
            vmCell.ViewModel.UpdateCellForKeyDown(vmCell.Index, App.Instance.ClipboardHelper.Value);
        }
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
        Grid.Loaded -= Grid_Loaded;
        Unloaded -= PuzzleView_Unloaded;
        SizeChanged -= PuzzleView_SizeChanged;

        viewModel = null;
        selectedCell = null;
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
            if (selectedCell is not null)
            {
                selectedCell.IsSelected = false;
            }

            selectedCell = cell;
        }
        else if (ReferenceEquals(selectedCell, cell))
        {
            selectedCell = null;
        }
    }

    public void FocusSelectedCell()
    {
        if (selectedCell is not null)
        {
            if (selectedCell.Parent is not null)
            {
                bool success = selectedCell.Focus(FocusState.Programmatic);
                Debug.Assert(success);
            }
            else
            {
                // When a TabSelectionChanged event is received the new content won't have finished being added to
                // the Tab's content presenter. Wait for a subsequent size changed event indicating that it's now valid.
                SizeChanged += PuzzleView_SizeChanged;
            }
        }

        void PuzzleView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged -= PuzzleView_SizeChanged;
            selectedCell?.Focus(FocusState.Programmatic);
        }
    }

    public void ResetOpacityTransitionForThemeChange()
    {
        Debug.Assert(!IsLoaded);
        // the next time this tab is loaded the opacity transition may need to be restarted
        Grid.Opacity = (themeWhenSelected != Utils.NormaliseTheme(Settings.Instance.Theme)) ? 0 : 1;
    }
}
