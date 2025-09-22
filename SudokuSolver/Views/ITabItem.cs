namespace SudokuSolver.Views;

internal interface ITabItem
{       
    string HeaderText { get; }
    void EnableMenuAccessKeys(bool enable);
    void Closed();
    int PassthroughCount { get; }
    void AddPassthroughContent(in RectInt32[] rects);
    void InvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args);
}
