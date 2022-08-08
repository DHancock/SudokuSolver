namespace Sudoku.ViewModels;

internal sealed class RelayCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Func<object?, bool> canExecute;
    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object?> execute) : this(execute, DefaultCanExecute)
    {
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    
    private static bool DefaultCanExecute(object? _) => true;

    public bool CanExecute(object? param) => canExecute(param);

    public void Execute(object? param) => execute(param);
}
