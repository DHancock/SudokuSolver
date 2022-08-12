namespace Sudoku.ViewModels;

internal sealed class CellList : ObservableCollection<Cell>
{
    private const int Length = 81;

    private Cell TempStore { get; set; }


    public CellList() : base()
    {
        for (int index = 0; index < Length; index++)
            this.Add(new Cell(index));

        TempStore = new Cell(-1);
    }



    public void UpdateFromModelCell(Models.Cell modelCell)
    {
        int index = modelCell.Index;

        Cell temp = TempStore;
        TempStore = this[index];

        temp.CopyFrom(modelCell);

        this[index] = temp;
    }
}
