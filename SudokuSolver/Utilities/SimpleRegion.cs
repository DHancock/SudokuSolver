using System.Drawing;

namespace SudokuSolver.Utilities;

internal class SimpleRegion
{
    private readonly Region region;

    public SimpleRegion(RectInt32 rect)
    {
        region = new Region(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
    }

    public RectInt32[] ToArray()
    {
        RectangleF[] scans = region.GetRegionScans(new System.Drawing.Drawing2D.Matrix());

        if (scans.Length == 0)
            return Array.Empty<RectInt32>();

        RectInt32[] result = new RectInt32[scans.Length];

        for (int index = 0; index < scans.Length; ++index)
        {
            result[index] = ConvertToRectInt32(scans[index]);
        }

        return result;
    }

    private static RectInt32 ConvertToRectInt32(RectangleF rect)
    {
        return new RectInt32(Convert.ToInt32(rect.X), Convert.ToInt32(rect.Y), Convert.ToInt32(rect.Width), Convert.ToInt32(rect.Height));
    }

    public void Subtract(RectInt32 rect)
    {
        region.Exclude(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
    }
}
