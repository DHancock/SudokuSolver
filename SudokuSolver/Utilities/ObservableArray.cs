namespace SudokuSolver.Utilities;

internal partial class ObservableArray<T> : ICollection<T>, INotifyCollectionChanged 
{
    private readonly T[] items;

    public ObservableArray(int length)
    {
        items = new T[length];
    }

    public int Count => items.Length;

    public T this[int index]
    {
        get => items[index];
        set => items[index] = value;
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public void RaiseCollectionChanged()
    {
        // As of WAS 1.8.1, the auto-generated code ignores the newItem, oldItem and index parameters.
        // It just updates the whole collection, so might as well batch changes and notify it once.
        // It could save up to 81x80 updates (6480)
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, this, this, 0));
    }

    public Span<T> AsSpan()
    {
        return new Span<T>(items);
    }

    // implementing ICollection<T> and having an indexer property is required for x:Bind
    public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    bool ICollection<T>.IsReadOnly => throw new NotImplementedException();
    bool ICollection<T>.Contains(T item) => throw new NotImplementedException();
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
    void ICollection<T>.Clear() => throw new NotImplementedException();
    bool ICollection<T>.Remove(T item) => throw new NotImplementedException();
    void ICollection<T>.Add(T item) => throw new NotImplementedException();
}