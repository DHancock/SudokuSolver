using System.ComponentModel;
using System.Runtime.CompilerServices;

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
                // always notify even if the value hasn't changed,
                // if its the same the Origin will be updated from Calculated to User
                base.Value = value;
                NotifyPropertyChanged(); 
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RevertValue(int value)
        {
            base.Value = value; // base doesn't fire a notification
        }
    }
}
