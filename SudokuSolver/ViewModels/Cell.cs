using System;
using System.ComponentModel;

using Sudoku.Common;

namespace Sudoku.ViewModels
{
    internal sealed class Cell : CellBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public Cell(int index, PropertyChangedEventHandler callBack) : base(index)
        {
            PropertyChanged += callBack ?? throw new ArgumentNullException(nameof(callBack));
        }



        public override int Value
        {
            set
            {
                if (base.Value != value)
                {
                    base.Value = value;
                    NotifyPropertyChanged(nameof(Value));
                }
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
