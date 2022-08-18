namespace Sudoku.ViewModels;

internal sealed class CellList : ObservableCollection<Cell>
{
    private const int cLength = 81;

    public CellList() : base()
    {
        for (int index = 0; index < cLength; index++)
            this.Add(new Cell(index));
    }
}
