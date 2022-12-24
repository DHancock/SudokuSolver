namespace Sudoku.Utilities;

internal static class Extensions
{
    public static bool IsEmpty(this RectInt32 rect)
    {
        Debug.Assert(rect.Height >= 0 && rect.Width >= 0);
        return rect.Height <= 0 || rect.Width <= 0;
    }

    public static Int32 Bottom(this RectInt32 rect)
    {
        Debug.Assert(rect.Height >= 0);
        return rect.Y + Math.Max(rect.Height, 0);
    }

    public static Int32 Right(this RectInt32 rect)
    {
        Debug.Assert(rect.Width >= 0);
        return rect.X + Math.Max(rect.Width, 0);
    }

    public static PointInt32 TopLeft(this RectInt32 rect)
    {
        return new PointInt32(rect.X, rect.Y);
    }

    // intersection algorithm from https://stackoverflow.com/a/306332
    // the trick is not to detect an intersection, but to prove that they cannot

    public static bool Intersects(this RectInt32 a, RectInt32 b)
    {
        return !a.DoesNotIntersect(b);
    }

    public static bool DoesNotIntersect(this RectInt32 a, RectInt32 b)
    {
        return a.X > b.Right() || a.Right() < b.X || a.Y > b.Bottom() || a.Bottom() < b.Y;
    }


    public static PointInt32 Offset(this PointInt32 a, int offset)
    {
        return new PointInt32(a.X + offset, a.Y + offset);
    }
}
