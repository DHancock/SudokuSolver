namespace Sudoku.Views;

/// <summary>
/// A minimal wrapper for a TextBlock because TextBlocks don't derive from a Control type. 
/// The VisualStateManager.GoToState() requires a control parameter so it isn't posssible
/// to call it for a TextBlock directly.
/// </summary>
internal sealed partial class PossibleTextBlock : UserControl
{
    public PossibleTextBlock()
    {
        this.InitializeComponent();
    }

    public string Text
    {
        set => PossibleValue.Text = value; 
    }
}
