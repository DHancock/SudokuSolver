namespace Sudoku.Utilities;

internal static class Utils
{
    public static async Task<BitmapImage> LoadEmbeddedImageResource(string resourcePath)
    {
        BitmapImage bitmapImage = new BitmapImage();

        using (Stream? resourceStream = typeof(App).Assembly.GetManifestResourceStream(resourcePath))
        {
            Debug.Assert(resourceStream is not null);

            using (IRandomAccessStream stream = resourceStream.AsRandomAccessStream())
            {
                await bitmapImage.SetSourceAsync(stream);
            }
        }

        return bitmapImage;
    }


    public class SimpleRegion
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


    // intersection algorithm from https://stackoverflow.com/a/306332
    // the trick is not to detect an intersection, but to prove they cannot

    public static bool Intersects(this RectInt32 a, RectInt32 b)  // !(a || b) == !a && !b
    {
        return a.X < b.Right() && a.Right() > b.X && a.Y < b.Bottom() && a.Bottom() > b.Y;
    }

    public static bool DoesNotIntersect(this RectInt32 a, RectInt32 b)
    {
        return a.X > b.Right() || a.Right() < b.X || a.Y > b.Bottom() || a.Bottom() < b.Y;
    }
}
