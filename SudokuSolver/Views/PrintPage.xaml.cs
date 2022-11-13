namespace Sudoku.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PrintPage : Page
{
    public PrintPage()
    {
        this.InitializeComponent();
        RequestedTheme = ElementTheme.Light;
    }

    public bool SetImageLocation(Point location) 
    {
        Debug.Assert(PrintableArea.Children.Count == 1);

        if (PrintableArea.Children.Count > 0)
        {
            FrameworkElement child = (FrameworkElement)PrintableArea.Children[0];

            if ((Canvas.GetLeft(child) != location.X) || (Canvas.GetTop(child) != location.Y))
            {
                Canvas.SetTop(child, location.Y);
                Canvas.SetLeft(child, location.X);
                return true;
            }
        }

        return false;
    }

    public void AddChild(FrameworkElement child)
    {
        Debug.Assert(PrintableArea.Children.Count == 0);
        PrintableArea.Children.Clear();
        PrintableArea.Children.Add(child);
    }

    public bool SetPageSize(Size size)
    {
        if ((PrintableArea.Width != size.Width) || (PrintableArea.Height != size.Height))
        {
            PrintableArea.Width = size.Width;
            PrintableArea.Height = size.Height;
            return true;
        }

        return false;
    }

    public bool SetImageSize(double size)
    {
        Debug.Assert(PrintableArea.Children.Count == 1);

        if (PrintableArea.Children.Count > 0)
        {
            FrameworkElement child = (FrameworkElement)PrintableArea.Children[0];

            if ((child.Width != size) || (child.Height != size))
            {
                child.Width = size;
                child.Height = size;
                return true;
            }
        }

        return false;
    }
}
