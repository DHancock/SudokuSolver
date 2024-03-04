namespace SudokuSolver.Views;

using Line = Microsoft.UI.Xaml.Shapes.Line;

internal sealed class SudokuGrid : Panel
{
    private double minorGridLineWidth = 0.0;
    private double majorGridLineWidth = 0.0;
    private double totalWidthOfBorders = 0.0;
    private double cellSize = 0.0;
    private double defaultMinorGridLineWidth = 0.0;

    public const int cCellsInRow = 9;
    public const int cCellCount = cCellsInRow * cCellsInRow;
    private const int cMinorGridLineCount = 12;
    private const int cMajorGridLineCount = 8;
    private const int cValidChildrenCount = cCellCount + cMinorGridLineCount + cMajorGridLineCount;

    public SudokuGrid() : base()
    {
        // Set layout rounding to false, otherwise the grid lines don't always align correctly, depending
        // on the screen scale factor. It's most noticeable on the bounding major grid lines when set to 125%. 
        UseLayoutRounding = false;
    }

    private void InitializeGridSizes()
    {
        // this assumes the xaml is correctly laid out and that all the major 
        // grid lines and minor grid lines each have the same stroke width
        defaultMinorGridLineWidth = minorGridLineWidth = ((Line)Children[cCellCount]).StrokeThickness;
        majorGridLineWidth = ((Line)Children[cCellCount + cMinorGridLineCount]).StrokeThickness;

        totalWidthOfBorders = (minorGridLineWidth * (cMinorGridLineCount / 2)) + (majorGridLineWidth * (cMajorGridLineCount / 2));
    }


    // Calculates the desired size of the grid
    protected override Size MeasureOverride(Size constraint)
    {
        // it's in a view box, all constraints will be infinite
        Debug.Assert(double.IsInfinity(constraint.Height) && double.IsInfinity(constraint.Width));

        if (Children.Count == cValidChildrenCount)
        {
            InitializeGridSizes();

            foreach (UIElement child in Children)
                child?.Measure(constraint);   // the child will update it's desired size

            // copy for future reference
            cellSize = Children[0].DesiredSize.Width;

            Size desiredSize = new Size();
            desiredSize.Width = desiredSize.Height = (cellSize * cCellsInRow) + totalWidthOfBorders;

            return desiredSize;
        }

        return Size.Empty;  // for design time only
    }

    // Define the layout of the child elements within the grid
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        if (Children.Count == cValidChildrenCount)
        {
            ArrangeCells();
            ArrangeGridLines(arrangeSize);
        }

        return arrangeSize;
    }

    private void ArrangeCells()
    {
        double[] offsets =
        [
            (majorGridLineWidth * 1),
            (majorGridLineWidth * 1) + (minorGridLineWidth * 1) + (cellSize * 1),
            (majorGridLineWidth * 1) + (minorGridLineWidth * 2) + (cellSize * 2),
            (majorGridLineWidth * 2) + (minorGridLineWidth * 2) + (cellSize * 3),
            (majorGridLineWidth * 2) + (minorGridLineWidth * 3) + (cellSize * 4),
            (majorGridLineWidth * 2) + (minorGridLineWidth * 4) + (cellSize * 5),
            (majorGridLineWidth * 3) + (minorGridLineWidth * 4) + (cellSize * 6),
            (majorGridLineWidth * 3) + (minorGridLineWidth * 5) + (cellSize * 7),
            (majorGridLineWidth * 3) + (minorGridLineWidth * 6) + (cellSize * 8),
        ];

        Rect finalRect = new Rect(0, 0, cellSize, cellSize);

        for (int index = 0; index < cCellCount; index++)
        {
            int x = index % cCellsInRow;
            int y = index / cCellsInRow;

            finalRect.X = offsets[x];
            finalRect.Y = offsets[y];

            Children[index].Arrange(finalRect);
        }
    }

    private void ArrangeGridLines(Size arrangeSize)
    {
        Rect finalRect = new Rect(0, 0, arrangeSize.Width, arrangeSize.Height);

        for (int index = 0; index < cMinorGridLineCount + cMajorGridLineCount; index += 2)
        {
            Line horizontalLine = (Line)Children[cCellCount + index];

            horizontalLine.Y1 = horizontalLine.Y2 = CalculateOffset(index);

            // the last four major grid lines have different start  
            // and end points because they form the enclosing rectangle
            horizontalLine.X1 = (index < 16) ? majorGridLineWidth : 0.0;
            horizontalLine.X2 = arrangeSize.Width - horizontalLine.X1;

            horizontalLine.Arrange(finalRect);

            // the vertical grid lines have the same dimensions as the horizontal
            // grid lines but rotated by 90 degrees - swap the x and y coordinates
            Line verticalLine = (Line)Children[cCellCount + index + 1];

            verticalLine.X1 = horizontalLine.Y1;
            verticalLine.Y1 = horizontalLine.X1;
            verticalLine.X2 = horizontalLine.Y2;
            verticalLine.Y2 = horizontalLine.X2;

            verticalLine.Arrange(finalRect);
        }
    }

    private double CalculateOffset(int index)
    {
        double CalculateOffset(int majorGridLines, int cells, int minorGridLines, double lineWidth)
        {
            return (majorGridLineWidth * majorGridLines) + (cellSize * cells) +
                    (minorGridLineWidth * minorGridLines) + (lineWidth * 0.5);
        }

        switch (index)
        {
            case 0: return CalculateOffset(1, 1, 0, minorGridLineWidth);
            case 2: return CalculateOffset(1, 2, 1, minorGridLineWidth);
            case 4: return CalculateOffset(2, 4, 2, minorGridLineWidth);
            case 6: return CalculateOffset(2, 5, 3, minorGridLineWidth);
            case 8: return CalculateOffset(3, 7, 4, minorGridLineWidth);
            case 10: return CalculateOffset(3, 8, 5, minorGridLineWidth);

            // internal major grid lines
            case 12: return CalculateOffset(1, 3, 2, majorGridLineWidth);
            case 14: return CalculateOffset(2, 6, 4, majorGridLineWidth);

            // major grid lines forming the enclosing rectangle 
            case 16: return CalculateOffset(0, 0, 0, majorGridLineWidth);
            case 18: return CalculateOffset(3, 9, 6, majorGridLineWidth);
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    // Adjust the thickness of the minor grid lines depending on the ViewBox
    // scale factor. When the shrinking the grid, the lines could be interpolated
    // out. Increasing their thickness ensures that they're always visible.
    public void AdaptForScaleFactor(double viewBoxWidth)
    {
        double newWidth = defaultMinorGridLineWidth;
        double epsilon = 0.001;
        double scaleFactor = viewBoxWidth / ActualSize.X;

        if (scaleFactor < 1.0)  
        {
            newWidth = defaultMinorGridLineWidth / scaleFactor;
            epsilon = 0.1; // limit changes, they cause a new measure pass
        }

        if (Math.Abs(((Line)Children[cCellCount]).StrokeThickness - newWidth) > epsilon)
        {
            for (int index = 0; index < cMinorGridLineCount; index++)
            {
                ((Line)Children[cCellCount + index]).StrokeThickness = newWidth;
            }
        }
    }
}
