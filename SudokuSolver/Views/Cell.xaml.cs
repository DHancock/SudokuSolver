using Sudoku.Common;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for Cell.xaml
/// </summary>
internal sealed partial class Cell : UserControl //, INotifyPropertyChanged
{
    private static readonly string[] sLookUp = new[] { string.Empty, "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    public event PropertyChangedEventHandler? PropertyChanged;

    private TextBlock[] PossibleTBs { get; }

    private Origins origin = Origins.NotDefined;



    public Cell()
    {
        this.InitializeComponent();

        IsTabStop = true;
        IsHitTestVisible = true;
        AllowFocusOnInteraction = true;
        ManipulationMode = ManipulationModes.System;

        LosingFocus += Cell_LosingFocus;

        PossibleTBs = new TextBlock[9] { PossibleValue0, PossibleValue1, PossibleValue2, PossibleValue3, PossibleValue4, PossibleValue5, PossibleValue6, PossibleValue7, PossibleValue8 };
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

    // Above the every thing else in the visual tree is a scroll viewer
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

    // the cell value origin notification is for a data trigger that controls 
    // properties of the displayed cell
    public Origins Origin
    {
        get => origin;

        set
        {
            if (value != origin)
            {
                origin = value;

                if (origin != Origins.NotDefined)
                {
                    bool stateFound = VisualStateManager.GoToState(this, origin.ToString(), false);
                    Debug.Assert(stateFound);
                }
            }
        }
    }


    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        cell.Origin = data.Origin;

        if (data.HasValue)
        {
            if (((ViewModels.PuzzleViewModel)cell.DataContext).ShowSolution || (data.Origin == Origins.User))
            {
                cell.CellValue.Text = sLookUp[data.Value];
            }
            else
                cell.CellValue.Text = string.Empty;

            foreach (TextBlock tb in cell.PossibleTBs)
                tb.Text = string.Empty;
        }
        else
        {
            cell.CellValue.Text = string.Empty;

            int writeIndex = 0;

            for (int i = 1; i < 10; i++)
            {
                if (data.Possibles[i])
                {
                    TextBlock tb = cell.PossibleTBs[writeIndex++];
                    tb.Text = sLookUp[i];

                    if (data.VerticalDirections[i])
                        tb.Foreground = new SolidColorBrush(Colors.Green);
                    else if (data.HorizontalDirections[i])
                        tb.Foreground = new SolidColorBrush(Colors.Red);
                    else
                        tb.Foreground = new SolidColorBrush(Colors.LightGray);
                }
            }

            while (writeIndex < 9)
                cell.PossibleTBs[writeIndex++].Text = string.Empty;
        }
    }



    // The view's cell.Data is bound to a view model's cell. When it's value
    // changes the view model passes the cell to the model which recalculates the puzzle.
    // After the models finished recalculating the view model's observable collection
    // of cells is compared to the model's cells and updated as required forcing
    // the UI to be updated.
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if ((e.Key > VirtualKey.Number0) && (e.Key <= VirtualKey.Number9))
        {
            Data.Value = e.Key - VirtualKey.Number0;
        }
        else if ((e.Key > VirtualKey.NumberPad0) && (e.Key <= VirtualKey.NumberPad9))
        {
            Data.Value = e.Key - VirtualKey.NumberPad0;
        }
        else if ((e.Key == VirtualKey.Delete) || (e.Key == VirtualKey.Back))
        {
            Data.Value = 0;
        }

        e.Handled = true;
    }
}
