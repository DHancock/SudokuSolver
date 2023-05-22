using SudokuSolver.Models;

namespace SudokuSolver.ViewModels;

internal sealed class UndoHelper
{
    private readonly LimitedSizeStack undoStack;
    private readonly LimitedSizeStack redoStack;
    private PuzzleModel? currentModel;

    public UndoHelper(int maxCount)
    {
        undoStack = new LimitedSizeStack(maxCount);
        redoStack = new LimitedSizeStack(maxCount);
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


    private sealed class LimitedSizeStack
    {
        private readonly int maxCount;
        private readonly LinkedList<PuzzleModel> list = new LinkedList<PuzzleModel>();

        public LimitedSizeStack(int maxCount)
        {
            this.maxCount = maxCount;
        }

        public void Push(PuzzleModel model)
        {
            list.AddFirst(model);

            if (list.Count > maxCount)
                list.RemoveLast();
        }

        public PuzzleModel Pop()
        {
            if (list.Count > 0)
            {
                PuzzleModel model = list.First();
                list.RemoveFirst();
                return model;
            }

            Debug.Fail("attempted Pop() from an empty list");
            return new PuzzleModel();
        }

        public void Clear() => list.Clear();

        public int Count => list.Count;
    }
}
