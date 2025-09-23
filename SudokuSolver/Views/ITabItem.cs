namespace SudokuSolver.Views;

internal interface ITabItem
{       
    string HeaderText { get; }
    void EnableAccessKeys(bool enable);
    void Closed();
    int PassthroughCount { get; }
    void AddPassthroughContent(in RectInt32[] rects);
    void InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key);
}
