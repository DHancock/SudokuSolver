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


    private static bool SetLocation(UIElement element, Point location)
    {
        if ((Canvas.GetLeft(element) != location.X) || (Canvas.GetTop(element) != location.Y))
        {
            Canvas.SetTop(element, location.Y);
            Canvas.SetLeft(element, location.X);
            return true;
        }

        return false;
    }

    public bool SetPuzzleLocation(Point location) => SetLocation(Puzzle, location);

    public bool SetHeadingLocation(Point location) => SetLocation(Header, location);

    public bool SetHeadingWidth(double width)
    {
        if (Header.Width != width)
        {
            Header.Width = width;
            return true;
        }

        return false;
    }

    internal void AddChild(PuzzleView child)
    {
        Debug.Assert(PageCanvas.Children.Count == 1);
        PageCanvas.Children.Add(child);
    }

    private static bool SetSize(FrameworkElement element, Size size)
    {
        if ((element.Width != size.Width) || (element.Height != size.Height))
        {
            element.Width = size.Width;
            element.Height = size.Height;
            return true;
        }

        return false;
    }

    public bool SetPageSize(Size size) => SetSize(this, size);

    public bool SetPuzzleSize(double size) => SetSize(Puzzle, new Size(size, size));

    private PuzzleView Puzzle
    {
        get
        {
            Debug.Assert(PageCanvas.Children.Count == 2);
            Debug.Assert(PageCanvas.Children[1] is PuzzleView);
            return (PuzzleView)PageCanvas.Children[1];
        }
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
