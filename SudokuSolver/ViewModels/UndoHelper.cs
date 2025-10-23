using SudokuSolver.Models;

namespace SudokuSolver.ViewModels;

internal sealed class UndoHelper
{
    private const int cMaxUndoCount = 20;

    private readonly UndoStack<PuzzleModel> undoStack;
    private readonly RedoStack<PuzzleModel> redoStack;
    private PuzzleModel? currentModel;

    public UndoHelper()
    {
        undoStack = new UndoStack<PuzzleModel>(cMaxUndoCount);
        redoStack = new RedoStack<PuzzleModel>(cMaxUndoCount);
    }

    public UndoHelper(UndoHelper source)
    {
        undoStack = new UndoStack<PuzzleModel>(source.undoStack);
        redoStack = new RedoStack<PuzzleModel>(source.redoStack);

        if (source.currentModel is not null)
        {
            currentModel = new PuzzleModel(source.currentModel);
        }
    }

    public void Push(PuzzleModel model)
    {
        // can only redo what's been undone
        redoStack.Clear();

        if (currentModel is not null)
        {
            undoStack.Push(currentModel);
        }

        currentModel = new PuzzleModel(model);
    }

    public PuzzleModel PopUndo()
    {
        if (currentModel is not null)
        {
            redoStack.Push(currentModel);
        }

        currentModel = undoStack.Pop();

        return new PuzzleModel(currentModel);
    }

    public PuzzleModel PopRedo()
    {
        if (currentModel is not null)
        {
            undoStack.Push(currentModel);
        }

        currentModel = redoStack.Pop();

        return new PuzzleModel(currentModel);
    }

    public bool CanUndo => undoStack.Count > 0;

    public bool CanRedo => redoStack.Count > 0;

    public void Reset()
    {
        undoStack.Clear();
        redoStack.Clear();
        currentModel = null;
    }

    private sealed class UndoStack<T> where T : new()
    {
        private readonly int maxCount;
        private readonly LinkedList<T> list = new LinkedList<T>();

        public UndoStack(int maxCount)
        {
            this.maxCount = maxCount;
        }

        public UndoStack(UndoStack<T> source) : this(source.maxCount)
        {
            LinkedListNode<T>? node = source.list.First;

            while (node is not null)
            {
                list.AddLast(new LinkedListNode<T>(node.Value));
                node = node.Next;
            }
        }

        public void Push(T item)
        {
            list.AddFirst(item);

            if (list.Count > maxCount)
            {
                list.RemoveLast();
            }
        }

        public T Pop()
        {
            if (list.Count > 0)
            {
                T item = list.First();
                list.RemoveFirst();
                return item;
            }

            Debug.Fail("attempted Pop() from an empty list");
            return new T();
        }

        public void Clear() => list.Clear();

        public int Count => list.Count;
    }


    private sealed class RedoStack<T> : Stack<T>
    {
        public RedoStack(int capacity): base(capacity) 
        { 
        }

        public RedoStack(RedoStack<T> source) : base(source.Capacity)
        {
            if (source.Count > 0)
            {
                T[] temp = source.ToArray();

                for (int index = source.Count - 1; index >= 0; index--)
                {
                    Push(temp[index]);
                }
            }
        }
    }
}
