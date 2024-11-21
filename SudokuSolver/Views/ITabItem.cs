namespace SudokuSolver.Views;

internal interface ITabItem
{       
    string HeaderText { get; }
    void AdjustKeyboardAccelerators(bool enable);
    void AdjustMenuAccessKeys(bool enable);
}
