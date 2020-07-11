using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Sudoku.Views
{
    /// <summary>
    /// Interaction logic for Cell.xaml
    /// </summary>
    public partial class Cell : UserControl, INotifyPropertyChanged
    {
        private static readonly string[] sLookUp = new[] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        public event PropertyChangedEventHandler PropertyChanged;


        public enum States { NoValue, UserValue, CalculatedValue }


        private States cellState = States.NoValue;



        public Cell()
        {
            InitializeComponent();

            Focusable = true;
            IsTabStop = true;

            MouseDown += Cell_MouseDown;
            KeyDown += Cell_KeyDown;
        }



        // the cell state notification is for a data trigger that controls 
        // properties of the displayed cell
        public States State
        {
            get => cellState;

            set
            {
                if (value != cellState)
                {
                    cellState = value;
                    NotifyPropertyChanged(nameof(State));
                }
            }
        }


        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }




        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data),
            typeof(ViewModels.Cell),
            typeof(Cell),
            new PropertyMetadata(CellDataChangedCallback));


        internal ViewModels.Cell Data
        {
            get { return (ViewModels.Cell)GetValue(DataProperty); }
            set { base.SetValue(DataProperty, value); }
        }



        private static void CellDataChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Cell cell = (Cell)d;
            ViewModels.Cell data = (ViewModels.Cell)e.NewValue;

            if (data.HasValue || (data.Possibles.Count == 1))
            {
                if (data.HasValue)
                {
                    cell.State = States.UserValue;
                    cell.CellValue.Text = sLookUp[data.Value];
                }
                else
                {
                    cell.State = States.CalculatedValue;
                    cell.CellValue.Text = sLookUp[data.Possibles.First];
                }

                cell.PossibleValue0.Text = string.Empty;
                cell.PossibleValue1.Text = string.Empty;
                cell.PossibleValue2.Text = string.Empty;
                cell.PossibleValue3.Text = string.Empty;
                cell.PossibleValue4.Text = string.Empty;
                cell.PossibleValue5.Text = string.Empty;
                cell.PossibleValue6.Text = string.Empty;
                cell.PossibleValue7.Text = string.Empty;
                cell.PossibleValue8.Text = string.Empty;
            }
            else
            {
                cell.CellValue.Text = string.Empty;
                cell.State = States.NoValue;

                int writeIndex = 1;

                for (int i = 1; i < 10; i++)
                {
                    if (data.Possibles[i])
                    {
                        TextBlock tb = cell.GetPossibleTextBlock(writeIndex++);
                        tb.Text = sLookUp[i];

                        if (data.VerticalDirections[i])
                        {
                            if (data.HorizontalDirections[i])
                                tb.Foreground = Brushes.Violet;  // both (can only be single possible)
                            else
                                tb.Foreground = Brushes.Green;
                        }
                        else if (data.HorizontalDirections[i])
                            tb.Foreground = Brushes.Red;
                        else
                            tb.Foreground = Brushes.LightGray;
                    }
                }

                while (writeIndex < 10)
                    cell.GetPossibleTextBlock(writeIndex++).Text = string.Empty;
            }
        }




        [SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "pointless")]
        private TextBlock GetPossibleTextBlock(int index)
        {
            switch (index)
            {
                case 1: return PossibleValue0;
                case 2: return PossibleValue1;
                case 3: return PossibleValue2;
                case 4: return PossibleValue3;
                case 5: return PossibleValue4;
                case 6: return PossibleValue5;
                case 7: return PossibleValue6;
                case 8: return PossibleValue7;
                case 9: return PossibleValue8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }


        // cell.Data is bound to the cell. Changing its Value property raises a
        // property changed notification which is listened to by the PuzzleViewModel
        // This passes the updated data to the model which recalculates the puzzle.
        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            Views.Cell cell = (Cell)sender;

            if ((e.Key > Key.D0) && (e.Key <= Key.D9))  // the Key enum explicitly states values
            {
                cell.Data.Value = e.Key - Key.D0;
                e.Handled = true;
            }
            else if ((e.Key == Key.Delete) || (e.Key == Key.Back))
            {
                cell.Data.Value = 0;
                e.Handled = true;
            }
        }


        private void Cell_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IInputElement element = (IInputElement)sender;
            Keyboard.Focus(element);
        }
    }
}
