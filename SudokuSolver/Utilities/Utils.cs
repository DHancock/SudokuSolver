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

            if (!rect.Intersects(subtracted))
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

    public static bool IsEmpty(this RectInt32 rect) => rect.Height <= 0 || rect.Width <= 0;

    public static Int32 Bottom(this RectInt32 rect) => rect.Y + Math.Max(rect.Height, 0);

    public static Int32 Right(this RectInt32 rect) => rect.X + Math.Max(rect.Width, 0);

    public static bool Intersects(this RectInt32 rect, RectInt32 other)
    {
        static bool Overlaps(RectInt32 a, RectInt32 b)
        {
            bool topInside = (a.Y >= b.Y) && (a.Y <= b.Bottom());
            bool leftInside = (a.X >= b.X) && (a.X <= b.Right());

            if (topInside && leftInside) // top left
                return true;

            bool bottomInside = (a.Bottom() >= b.Y) && (a.Bottom() <= b.Bottom());

            if (bottomInside && leftInside)  // bottom left
                return true;

            bool rightInside = (a.Right() >= b.X) && (a.Right() <= b.Right());

            // bottom right || top right
            return rightInside && (bottomInside || topInside);
        }

        return Overlaps(rect, other) || Overlaps(other, rect); // check for the a encloses b case
    }
}
