using System;
using System.ComponentModel;

using Sudoku.Common;

namespace Sudoku.ViewModels
{
    internal sealed class Cell : CellBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Cell(int index, PropertyChangedEventHandler callBack) : base(index)
        {
            PropertyChanged += callBack;
        }

        public override int Value
        {
            set
            {
                base.Value = value;
                NotifyPropertyChanged(nameof(Value)); // always notify even if the value hasn't changed
            }
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RevertValue(int value)
        {
            base.Value = value; // base doesn't fire a notification
        }
    }
}
