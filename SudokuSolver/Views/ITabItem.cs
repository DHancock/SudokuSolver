namespace SudokuSolver.Views;

internal interface ITabItem
{
    void AdjustKeyboardAccelerators(bool enable);
    void UpdateContextMenuItemsEnabledState();
}
