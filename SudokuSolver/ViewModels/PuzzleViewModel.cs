using System;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows.Input;
using System.Runtime.CompilerServices;

using Sudoku.Common;
using Sudoku.Models;


namespace Sudoku.ViewModels
{
    internal sealed class PuzzleViewModel : INotifyPropertyChanged
    {
        private bool showPossibles = false;
        private bool darkThemed = false;
        private bool accentTitleBar = false;

        private PuzzleModel Model { get; }
        public CellList Cells { get; }
        public ICommand ClearCommand { get; }


        public PuzzleViewModel()
        {
            Model = new PuzzleModel();
            Cells = new CellList(CellChanged_EventHandler);
            ClearCommand = new RelayCommand(ClearCommandHandler, o => Cells.NotEmpty);
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

                if (previousValue != newValue)
                {
                    if (origin == Origins.User)
                        return Model.Edit;

                    if ((origin == Origins.Trial) || (origin == Origins.Calculated))
                        return Model.EditForced;
                }

                if ((previousValue == newValue) && ((origin == Origins.Trial) || (origin == Origins.Calculated)))
                    return Model.SetOrigin;
            }

            if ((newValue == 0) && (previousValue > 0) && (origin == Origins.User))
                return Model.Delete;

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
                    changedCell.Value = -1;   // forces a cell update
                    UpdateView();
                }
                else
                {
                    changedCell.RevertValue(previousValue); // avoids another cell changed event
                    SystemSounds.Beep.Play();
                }
            }
            else
                changedCell.RevertValue(previousValue);
        }


        public void Save(Stream stream) => Model.Save(stream);
    

        public void Open(Stream stream)
        {
            try
            {
                Model.Clear();
                Model.Open(stream);
            }
            catch
            {
                Model.Clear();
                throw;
            }
            finally
            {
                UpdateView();
            }
        }


        private void UpdateView()
        {
            // copy model cells in to the view model observable collection, causing a ui update
            foreach (Models.Cell cell in Model.Cells) 
            {
                if (!cell.Equals(Cells[cell.Index]))
                    Cells.UpdateCell(cell);
            }
        }


        private void ClearCommandHandler(object? _)
        {
            Model.Clear();
            UpdateView();
        }


        public bool ShowPossibles
        {                                                        
            get => showPossibles;
            set
            {
                if (value != showPossibles)
                {
                    showPossibles = value;
                    NotifyPropertyChanged();
                }
            }
        }

        
        public bool DarkThemed
        {
            get => darkThemed;
            set
            {
                if (value != darkThemed)
                {
                    darkThemed = value;
                    NotifyPropertyChanged();
                }
            }
        }


        public bool AccentTitleBar
        {
            get => accentTitleBar;
            set
            {
                if (value != accentTitleBar)
                {
                    accentTitleBar = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
