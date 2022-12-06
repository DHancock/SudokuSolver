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
            rects.Add(rect);
        }

        public RectInt32[] ToArray() => rects.ToArray();

        public void Subtract(RectInt32 subtracted)
        {
            List<RectInt32> results = new List<RectInt32>();

            foreach (RectInt32 rect in rects)
            {
                IEnumerable<RectInt32> subRects = Subtract(rect, subtracted);
                results.AddRange(subRects);
            }

            rects.Clear();
            rects.AddRange(results);
        }

        private static IEnumerable<RectInt32> Subtract(RectInt32 rect, RectInt32 subtracted)
        {
            List<RectInt32> results = new List<RectInt32>();

            if (rect.IsEmpty() || rect.Equals(subtracted))
            {
                results.Add(new RectInt32(rect.X, rect.Y, 0, 0));
                return results;
            }

            if (subtracted.IsEmpty() || !rect.Intersects(subtracted))
            {
                results.Add(rect);
                return results;
            }

            Int32 heightA = subtracted.Y - rect.Y;

            if (heightA > 0)
                results.Add(new RectInt32(rect.X, rect.Y, rect.Width, heightA));

            Int32 widthB = subtracted.X - rect.X;

            if (widthB > 0)
                results.Add(new RectInt32(rect.X, subtracted.Y, widthB, subtracted.Height));

            Int32 widthC = rect.Right() - subtracted.Right();

            if (widthC > 0)
                results.Add(new RectInt32(subtracted.Right(), subtracted.Y, widthC, subtracted.Height));

            Int32 heightD = rect.Bottom() - subtracted.Bottom();

            if (heightD > 0)
                results.Add(new RectInt32(rect.X, subtracted.Bottom(), rect.Width, heightD));

            return results;
        }
    }

    public static bool IsEmpty(this RectInt32 rect) => rect.Height <= 0 || rect.Width <= 0;

    public static Int32 Bottom(this RectInt32 rect) => rect.Y + rect.Height;

    public static Int32 Right(this RectInt32 rect) => rect.X + rect.Width;

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

            if (bottomInside && rightInside)  // bottom right
                return true;

            if (topInside && rightInside) // top right
                return true;

            return false;
        }

        return Overlaps(rect, other) || Overlaps(other, rect);
    }

    public static void Scale(this RectInt32 rect, double scale)
    {
        rect.X = Convert.ToInt32(rect.X * scale);
        rect.Y = Convert.ToInt32(rect.Y * scale);
        rect.Width = Convert.ToInt32(rect.Width * scale);
        rect.Height = Convert.ToInt32(rect.Height * scale);
    }
}
