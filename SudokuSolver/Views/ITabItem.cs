namespace SudokuSolver.Views;

internal interface ITabItem
{       
    string HeaderText { get; }
    void EnableKeyboardAccelerators(bool enable);
    void EnableMenuAccessKeys(bool enable);
}
