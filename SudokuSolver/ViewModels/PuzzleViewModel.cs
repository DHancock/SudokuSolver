using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

using Sudoku.Common;
using Sudoku.Models;

namespace Sudoku.ViewModels
{
    internal sealed class PuzzleViewModel : INotifyPropertyChanged
    {
        // according to https://fileinfo.com this extension isn't in use (at least by a popular program)
        private const string cFileFilter = "Sudoku files|*.sdku";
        public const string cDefaultFileExt = ".sdku";

        private const string cDefaultWindowTitle = "Sudoku Solver";
        private string windowTitle = cDefaultWindowTitle;

        private bool showPossibles = false;

        private PuzzleModel Model { get; }
        public CellList Cells { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand PrintCommand { get; }
        

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
        private void CellChanged_EventHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null)
                return;

            Cell changedCell = (Cell)sender;

            int previousValue = Model.Cells[changedCell.Index].Value;
            Origins previousOrigin = changedCell.Origin;

            // if replacing an user cell value, first delete the old value to
            // recalculate the cell possibles which are then used to validate the new value
            if ((previousOrigin == Origins.User) && changedCell.HasValue)
                Model.SetCellValue(changedCell.Index, 0, Origins.NotDefined);

            if (Model.ValidateNewCellValue(changedCell.Index, changedCell.Value))
            {
                Model.SetCellValue(changedCell.Index, changedCell.Value, changedCell.HasValue ? Origins.User : Origins.NotDefined);

                Model.AttemptSimpleTrialAndError();

                foreach (Models.Cell cell in Model.Cells)
                {
                    if (!cell.Equals(Cells[cell.Index]) || (changedCell.Index == cell.Index))
                        Cells.UpdateCell(cell);
                }
            }
            else  // revert the cell's value, updating the model if required 
            {
                changedCell.RevertValue(previousValue); // avoids another cell changed event

                if (previousOrigin == Origins.User)
                    Model.SetCellValue(changedCell.Index, previousValue, Origins.User);

                SystemSounds.Beep.Play();
            }
        }


        private void SaveCommandHandler(object? _)
        {
            SaveFileDialog dialog = new SaveFileDialog { Filter = cFileFilter };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using Stream stream = dialog.OpenFile();
                    Model.Save(stream);
                }
                catch (Exception e)
                {
                    // TODO - failed to save message box
                    Debug.Fail(e.Message);
                }
            }
        }


        private void OpenCommandHandler(object? _)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = cFileFilter };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using Stream stream = dialog.OpenFile();
                    OpenFile(stream, dialog.FileName);
                }
                catch (Exception e)
                {
                    // TODO - failed to open message box
                    Debug.Fail(e.Message);
                }
            }
        }


        public void OpenFile(Stream stream, string fileName)
        {
            try
            {
                Model.Clear();
                Model.Open(stream);
                WindowTitle = cDefaultWindowTitle + " - " + Path.GetFileNameWithoutExtension(fileName);
            }
            catch
            {
                Model.Clear();
                WindowTitle = cDefaultWindowTitle;
                throw;
            }
            finally
            {
                foreach (Models.Cell cell in Model.Cells)
                    Cells.UpdateCell(cell);
            }
        }



        private void ClearCommandHandler(object? _)
        {
            Model.Clear();

            foreach (Models.Cell cell in Model.Cells)
                Cells.UpdateCell(cell);
        }


        private void PrintCommandHandler(object? _)
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



        public bool ShowPossibles
        {                                                        
            get => showPossibles;
            set
            {
                if (value != showPossibles)
                {
                    showPossibles = value;
                    NotifyPropertyChanged(nameof(ShowPossibles));
                }
            }
        }



        public string WindowTitle
        {
            get => windowTitle;

            private set
            {
                windowTitle = value;
                NotifyPropertyChanged(nameof(WindowTitle));
            }
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
