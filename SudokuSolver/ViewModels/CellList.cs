using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Sudoku.ViewModels
{
    internal sealed class CellList : ObservableCollection<Cell>
    {
        private const int Length = 81;

        private Cell TempStore { get; set; }


        public CellList(PropertyChangedEventHandler cellChangedEventHandler) : base()
        {
            for (int index = 0; index < Length; index++)
                this.Add(new Cell(index, cellChangedEventHandler));

            TempStore = new Cell(0, cellChangedEventHandler);
        }



        public void UpdateCell(Models.Cell modelCell)
        {
            int index = modelCell.Index;

            Cell temp = TempStore;
            TempStore = this[index];

            temp.CopyFrom(modelCell, index);

            this[index] = temp;
        }
    }
}
