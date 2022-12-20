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


    public bool SetLocation(UIElement element, Point location)
    {
        if ((Canvas.GetLeft(element) != location.X) || (Canvas.GetTop(element) != location.Y))
        {
            Canvas.SetTop(element, location.Y);
            Canvas.SetLeft(element, location.X);
            return true;
        }

        return false;
    }

    public bool SetPuzzleLocation(Point location) 
    {
        Debug.Assert(PageCanvas.Children.Count == 2);
        return SetLocation(PageCanvas.Children[1], location);
    }

    public bool SetHeadingLocation(Point location)
    {
        return SetLocation(Header, location);
    }

    public bool SetHeadingWidth(double width)
    {
        if (Header.Width != width)
        {
            Header.Width = width;
            return true;
        }

        return false;
    }

    public void AddChild(FrameworkElement child)
    {
        Debug.Assert(PageCanvas.Children.Count == 1);
        PageCanvas.Children.Add(child);
    }

    public bool SetPageSize(Size size)
    {
        if ((Width != size.Width) || (Height != size.Height))
        {
            Width = size.Width;
            Height = size.Height;
            return true;
        }

        return false;
    }

    public bool SetPuzzleSize(double size)
    {
        Debug.Assert(PageCanvas.Children.Count == 2);
        FrameworkElement child = (FrameworkElement)PageCanvas.Children[1];

        if ((child.Width != size) || (child.Height != size))
        {
            child.Width = size;
            child.Height = size;
            return true;
        }

        return false;
    }

    public bool ShowHeader(bool showHeader)
    {
        Visibility target = showHeader ? Visibility.Visible : Visibility.Collapsed;

        if (Header.Visibility != target)
        {
            Header.Visibility = target;
            return true;
        }
    
        return false;
    }

    public void SetHeaderText(string? text) => Header.Text = text ?? string.Empty;

    public double GetHeaderHeight() => Header.Height;
}
