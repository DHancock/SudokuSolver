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


        // an empty implementation used to indicate no action is required
        // when the user changes a cell. As the code supports nullable using
        // this avoids the need to return a null function delegate.
        private static bool NoOp(int index, int value)
        {
            throw new NotImplementedException();
        }


        private Func<int, int, bool> DetermineChange(Cell changedCell, int previousValue)
        {
            int newValue = changedCell.Value;
            Origins origin = changedCell.Origin;

            if (newValue > 0)
            {
                if (previousValue == 0) 
                    return Model.Add;

                if ((previousValue == newValue) && ((origin == Origins.Trial) || (origin == Origins.Calculated)))
                    return Model.SetOrigin;
            }

            if (previousValue > 0)
            {
                if ((newValue == 0) && (origin == Origins.User))
                    return Model.Delete;

                if ((newValue > 0) && (previousValue != newValue))
                {
                    if (origin == Origins.User)
                        return Model.Edit;

                    if ((origin == Origins.Trial) || (origin == Origins.Calculated))
                        return Model.EditForced;
                }
            }

            // typical changes that require no action are deleting an empty cell, 
            // deleting a cell containing a calculated value which would then just 
            // be recalculated and also editing a user cell to have the same value.
            return NoOp;
        }


        // the user typed a value into a cell
        private void CellChanged_EventHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null)
                return;

            Cell changedCell = (Cell)sender;
            int previousValue = Model.Cells[changedCell.Index].Value;

            Func<int, int, bool> modelFunction = DetermineChange(changedCell, previousValue);

            if (modelFunction != NoOp)
            {
                if (modelFunction(changedCell.Index, changedCell.Value))
                {
                    foreach (Models.Cell cell in Model.Cells) // copy model in to the view model
                    {
                        if (!cell.Equals(Cells[cell.Index]) || (changedCell.Index == cell.Index))
                            Cells.UpdateCell(cell);
                    }
                }
                else
                {
                    changedCell.RevertValue(previousValue); // avoids another cell changed event
                    SystemSounds.Beep.Play();
                }
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
