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
        set
        {
            T original = items[index];
            items[index] = value;

            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Replace, value, original, index));
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public IEnumerator<T> GetEnumerator() => new Enumerator(items);
    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();


    // implementing ICollection<T> and having an indexer property is required for x:Bind
    bool ICollection<T>.IsReadOnly => throw new NotImplementedException();
    bool ICollection<T>.Contains(T item) => throw new NotImplementedException();
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
    void ICollection<T>.Clear() => throw new NotImplementedException();
    bool ICollection<T>.Remove(T item) => throw new NotImplementedException();
    void ICollection<T>.Add(T item) => throw new NotImplementedException();


    private struct Enumerator : IEnumerator<T>
    {
        private readonly T[] array;
        private int index = -1;

        internal Enumerator(T[] array)
        {
            this.array = array;
        }

        public readonly void Dispose() { }

        public bool MoveNext() => ++index < array.Length;

        public readonly T Current => array[index];

        readonly object? IEnumerator.Current => Current;

        void IEnumerator.Reset() => index = -1;
    }
}