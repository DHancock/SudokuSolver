using SudokuSolver.Models;

namespace SudokuSolver.ViewModels;

internal sealed class UndoHelper
{
    private const int cMaxUndoCount = 20;

    private readonly UndoStack<PuzzleModel> undoStack;
    private readonly Stack<PuzzleModel> redoStack;
    private PuzzleModel? currentModel;

    public UndoHelper()
    {
        undoStack = new UndoStack<PuzzleModel>(cMaxUndoCount);
        redoStack = new Stack<PuzzleModel>(cMaxUndoCount);
    }

    public void Push(PuzzleModel model)
    {
        // can only redo what's been undone
        redoStack.Clear();

        if (currentModel is not null)
            undoStack.Push(currentModel);

        currentModel = new PuzzleModel(model);
    }

    public PuzzleModel PopUndo()
    {
        if (currentModel is not null)
            redoStack.Push(currentModel);

        currentModel = undoStack.Pop();

        return new PuzzleModel(currentModel);
    }

    public PuzzleModel PopRedo()
    {
        if (currentModel is not null)
            undoStack.Push(currentModel);

        currentModel = redoStack.Pop();

        return new PuzzleModel(currentModel);
    }

    public bool CanUndo => undoStack.Count > 0;

    public bool CanRedo => redoStack.Count > 0;


    private sealed class UndoStack<T> where T : new()
    {
        private readonly int maxCount;
        private readonly LinkedList<T> list = new LinkedList<T>();

        public UndoStack(int maxCount)
        {
            this.maxCount = maxCount;
        }

        public void Push(T item)
        {
            list.AddFirst(item);

            if (list.Count > maxCount)
                list.RemoveLast();
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

        public int Count => list.Count;
    }
}
