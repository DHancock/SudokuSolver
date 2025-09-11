namespace SudokuSolver.Views;

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
