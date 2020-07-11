using System;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

using Sudoku.Models;


namespace Sudoku.ViewModels
{
    internal sealed class PuzzleViewModel
    {
        // according to https://fileinfo.com this extension isn't in use (at least by a popular program)
        private const string cFileFilter = "Sudoku files (.sdku)|*.sdku";
        private const string cDefaultFileExt = ".sdku";

        private PuzzleModel Model { get; }
        public CellList Cells { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand PrintCommand { get; }
        public bool ShowPossibles { get; set; } = true;


        public PuzzleViewModel()
        {
            Model = new PuzzleModel();
            Cells = new CellList(CellChanged_EventHandler);

            OpenCommand = new RelayCommand(OpenCommandHandler);
            SaveCommand = new RelayCommand(SaveCommandHandler);
            ClearCommand = new RelayCommand(ClearCommandHandler, o => Cells.NotEmpty);
            PrintCommand = new RelayCommand(PrintCommandHandler);
        }


        // the user typed a value into a cell
        private void CellChanged_EventHandler(object sender, PropertyChangedEventArgs e)
        {
            Cell changedCell = (Cell)sender;

            int previousValue = Model.Cells[changedCell.Index].Value;

            // if replacing an old cell value with a new one, first delete the old value
            // to recalculate the cell possibles which are used to validate the new value
            if ((previousValue > 0) && changedCell.HasValue)
                Model.SetCellValue(changedCell.Index, 0);

            if (Model.ValidateCellValue(changedCell.Index, changedCell.Value))
            {
                Model.SetCellValue(changedCell.Index, changedCell.Value);

                foreach (Models.Cell cell in Model.Cells)
                {
                    if (!cell.Equals(Cells[cell.Index]) || (changedCell.Index == cell.Index))
                        Cells.UpdateCell(cell);
                }
            }
            else  // revert the cell's value, updating the model if required 
            {
                changedCell.RevertValue(previousValue); // avoids another cell changed event

                if (previousValue > 0)
                    Model.SetCellValue(changedCell.Index, previousValue);

                SystemSounds.Beep.Play();
            }
        }



        private void SaveCommandHandler(object o)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                DefaultExt = cDefaultFileExt,
                Filter = cFileFilter,
            };

            if (dialog.ShowDialog() == true)
                Model.Save(dialog.OpenFile());
        }



        private void OpenCommandHandler(object o)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                DefaultExt = cDefaultFileExt,
                Filter = cFileFilter,
            };

            if (dialog.ShowDialog() == true)
            {
                Model.Open(dialog.OpenFile());

                foreach (Models.Cell cell in Model.Cells)
                    Cells.UpdateCell(cell);
            }
        }


        private void ClearCommandHandler(object o)
        {
            Model.Clear();

            foreach (Models.Cell cell in Model.Cells)
                this.Cells.UpdateCell(cell);
        }


        private void PrintCommandHandler(object o)
        {
            PrintDialog printDialog = new PrintDialog
            {
                UserPageRangeEnabled = false,
                CurrentPageEnabled = false
            };

            if (printDialog.ShowDialog() == true)
            {
                const double cMarginsPercentage = 6.25;

                Views.PuzzleView puzzleView = new Views.PuzzleView
                {
                    Margin = new Thickness(Math.Min(printDialog.PrintableAreaHeight, printDialog.PrintableAreaWidth) * (cMarginsPercentage / 100D)),
                    DataContext = this
                };

                printDialog.PrintVisual(puzzleView, "Sudoku puzzle");
            }
        }
    }
}
