namespace SudokuSolver.Utilities;

internal class SimpleRegion
{
    private readonly List<RectInt32> rects = new List<RectInt32>();

    public SimpleRegion(RectInt32 rect)
    {
        if (!rect.IsEmpty())
            rects.Add(rect);
    }

    public RectInt32[] ToArray() => rects.ToArray();

    public void Subtract(RectInt32 subtracted)
    {
        if (subtracted.IsEmpty() || (rects.Count == 0))
            return;

        List<RectInt32> results = new List<RectInt32>();

        foreach (RectInt32 rect in rects)
            Subtract(rect, subtracted, results);

        rects.Clear();
        rects.AddRange(results);
    }

    public void Add(RectInt32 rect)
    {
        if (!rect.IsEmpty())
        {
            Subtract(rect);
            rects.Add(rect);
        }
    }

    private static void Subtract(RectInt32 rect, RectInt32 subtracted, List<RectInt32> results)
    {
        if (rect.Equals(subtracted))
            return;

        if (rect.DoesNotIntersect(subtracted))
        {
            results.Add(rect);
            return;
        }

        Int32 top = subtracted.Y - rect.Y;
        Int32 left = subtracted.X - rect.X;
        Int32 right = rect.Right() - subtracted.Right();
        Int32 bottom = rect.Bottom() - subtracted.Bottom();

        if ((top < 0) && (left < 0) && (right < 0) && (bottom < 0))
            return;
 
        if (top > 0)
            results.Add(new RectInt32(rect.X, rect.Y, rect.Width, top));

        if (left > 0)
            results.Add(new RectInt32(rect.X, subtracted.Y, left, subtracted.Height));

        if (right > 0)
            results.Add(new RectInt32(subtracted.Right(), subtracted.Y, right, subtracted.Height));

        if (bottom > 0)
            results.Add(new RectInt32(rect.X, subtracted.Bottom(), rect.Width, bottom));
    }
}