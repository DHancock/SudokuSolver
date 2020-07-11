using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sudoku.Views
{
    internal sealed class SudokuGrid : Panel
    {

        public static readonly DependencyProperty CellBorderThicknessProperty =
            DependencyProperty.Register(nameof(CellBorderThickness),
                typeof(double),
                typeof(SudokuGrid),
                new FrameworkPropertyMetadata(0.5D,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                SudokuGrid.ValidateThickness);


        public static readonly DependencyProperty CellBorderBrushProperty =
            DependencyProperty.Register(nameof(CellBorderBrush),
                typeof(Brush),
                typeof(SudokuGrid),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xFF, 0x88, 0x88, 0x88)),
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));




        public static readonly DependencyProperty CubeBorderThicknessProperty =
            DependencyProperty.Register(nameof(CubeBorderThickness),
                typeof(double),
                typeof(SudokuGrid),
                new FrameworkPropertyMetadata(2.0D,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                new ValidateValueCallback(SudokuGrid.ValidateThickness));


        public static readonly DependencyProperty CubeBorderBrushProperty =
            DependencyProperty.Register(nameof(CubeBorderBrush),
                typeof(Brush),
                typeof(SudokuGrid),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00)),
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));



        public double CellBorderThickness
        {
            get { return (double)GetValue(SudokuGrid.CellBorderThicknessProperty); }
            set { base.SetValue(SudokuGrid.CellBorderThicknessProperty, value); }
        }


        public Brush CellBorderBrush
        {
            get { return (Brush)GetValue(SudokuGrid.CellBorderBrushProperty); }
            set { base.SetValue(SudokuGrid.CellBorderBrushProperty, value); }
        }



        public double CubeBorderThickness
        {
            get { return (double)GetValue(SudokuGrid.CubeBorderThicknessProperty); }
            set { base.SetValue(SudokuGrid.CubeBorderThicknessProperty, value); }
        }

        public Brush CubeBorderBrush
        {
            get { return (Brush)GetValue(SudokuGrid.CubeBorderBrushProperty); }
            set { base.SetValue(SudokuGrid.CubeBorderBrushProperty, value); }
        }



        // pretty arbitrary but seems reasonable limits
        private static bool ValidateThickness(object o)
        {
            double d = (double)o;
            return !double.IsNaN(d) && (d >= 0.1) && (d <= 5.00);
        }


        private double TotalWidthOfBorders() => (4.0 * CubeBorderThickness) + (6.0 * CellBorderThickness);


        // Calculates the desired size of the grid
        protected override Size MeasureOverride(Size constraint)
        {

            double constraintSize = Math.Min(constraint.Width, constraint.Height);

            double borderThickness = TotalWidthOfBorders();

            double childSize = (constraintSize - borderThickness) / 9.0;

            Size availableSize = new Size(childSize, childSize);


            foreach (UIElement child in InternalChildren)
            {
                child.Measure(availableSize);   // causes the child to update it's desired size
            }

            Size desiredSize;

            if (InternalChildren.Count > 0)
                desiredSize = base.InternalChildren[0].DesiredSize;  // all cells are the same size
            else
                desiredSize = Size.Empty;  // for design time only

            desiredSize.Width = (desiredSize.Width * 9.0) + borderThickness;
            desiredSize.Height = desiredSize.Width;

            return desiredSize;
        }





        // Define the layout of the child elements within the grid
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double borderThickness = TotalWidthOfBorders();

            double cellSize = (DesiredSize.Width - borderThickness) / 9.0;

            double[] offsets = new double[9];

            offsets[0] = CubeBorderThickness;
            offsets[1] = offsets[0] + CellBorderThickness + cellSize;
            offsets[2] = offsets[1] + CellBorderThickness + cellSize;
            offsets[3] = offsets[2] + CubeBorderThickness + cellSize;
            offsets[4] = offsets[3] + CellBorderThickness + cellSize;
            offsets[5] = offsets[4] + CellBorderThickness + cellSize;
            offsets[6] = offsets[5] + CubeBorderThickness + cellSize;
            offsets[7] = offsets[6] + CellBorderThickness + cellSize;
            offsets[8] = offsets[7] + CellBorderThickness + cellSize;

            int index = 0;
            Rect finalRect = new Rect(0, 0, cellSize, cellSize);

            foreach (UIElement uIElement in InternalChildren)
            {
                int x = index % 9;
                int y = index / 9;

                finalRect.X = offsets[x];
                finalRect.Y = offsets[y];

                uIElement.Arrange(finalRect);
                ++index;
            }

            return arrangeSize;
        }


        protected override void OnRender(DrawingContext dc)
        {
            // draw children
            base.OnRender(dc);

            if (InternalChildren.Count == 0) // for design time only 
                return;

            // draw grids
            // the horizontal grid lines have the same dimensions as the vertical
            // grid lines but rotated by 90 degrees - swap x and y coordinates

            if ((CellBorderThickness > 0.0) && (CellBorderBrush != null))
            {
                Pen pen = new Pen(CellBorderBrush, CellBorderThickness);

                double n = pen.Thickness * 0.5;
                double cellWidth = InternalChildren[0].RenderSize.Width;

                Point verticalA = new Point(0.0, CubeBorderThickness);  // left vertical top 
                Point verticalB = new Point(0.0, (CubeBorderThickness * 3.0) + (cellWidth * 9.0) + (CellBorderThickness * 6.0));  // left vertical bottom

                Point horizontalA = new Point(verticalA.Y, verticalA.X); // top horizontal left
                Point horizontalB = new Point(verticalB.Y, verticalB.X); // top horizontal right

                double[] offsets = new double[6];

                offsets[0] = CubeBorderThickness + cellWidth + n;
                offsets[1] = offsets[0] + cellWidth + CellBorderThickness;
                offsets[2] = offsets[1] + (cellWidth * 2) + CubeBorderThickness + CellBorderThickness;
                offsets[3] = offsets[2] + cellWidth + CellBorderThickness;
                offsets[4] = offsets[3] + (cellWidth * 2) + CubeBorderThickness + CellBorderThickness;
                offsets[5] = offsets[4] + cellWidth + CellBorderThickness;

                for (int index = 0; index < 6; ++index)
                {
                    Vector xOffset = new Vector(offsets[index], 0.0);
                    Vector yOffset = new Vector(0.0, offsets[index]);

                    dc.DrawLine(pen, Point.Add(verticalA, xOffset), Point.Add(verticalB, xOffset));
                    dc.DrawLine(pen, Point.Add(horizontalA, yOffset), Point.Add(horizontalB, yOffset));
                }
            }


            if ((CubeBorderThickness > 0.0) && (CubeBorderBrush != null))
            {
                Pen pen = new Pen(CubeBorderBrush, CubeBorderThickness);

                double n = pen.Thickness * 0.5;
                double cellWidth = InternalChildren[0].RenderSize.Width;

                Point verticalA = new Point(n, 0.0);  // left vertical top
                Point verticalB = new Point(n, DesiredSize.Height);  // left vertical bottom

                Point horizontalA = new Point(verticalA.Y, verticalA.X); // top horizontal left
                Point horizontalB = new Point(verticalB.Y, verticalB.X); // top horizontal right

                double[] offsets = new double[4];

                offsets[0] = 0.0;
                offsets[1] = pen.Thickness + (CellBorderThickness * 2.0) + (cellWidth * 3.0);
                offsets[2] = offsets[1] * 2.0;
                offsets[3] = offsets[1] * 3.0;

                for (int index = 0; index < 4; ++index)
                {
                    Vector xOffset = new Vector(offsets[index], 0.0);
                    Vector yOffset = new Vector(0.0, offsets[index]);

                    dc.DrawLine(pen, Point.Add(verticalA, xOffset), Point.Add(verticalB, xOffset));
                    dc.DrawLine(pen, Point.Add(horizontalA, yOffset), Point.Add(horizontalB, yOffset));
                }
            }
        }
    }
}
