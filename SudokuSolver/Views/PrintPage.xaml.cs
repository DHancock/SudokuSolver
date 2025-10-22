using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class PrintPage : UserControl
{
    private PrintPage()
    {
        this.InitializeComponent();
    }

    public PrintPage(XElement root) : this()
    {
        RequestedTheme = ElementTheme.Light;
        Puzzle.ViewModel = new PuzzleViewModel();

        XElement? data = root.Element("title");

        if (data is not null)
        {
            Header.Text = data.Value;
        }

        data = root.Element("showPossibles");

        if (data is not null)
        {
            Puzzle.ViewModel.ShowPossibles = data.Value == "true";
        }

        data = root.Element("showSolution");

        if (data is not null)
        {
            Puzzle.ViewModel.ShowSolution = data.Value == "true";
        }

        data = root.Element("Sudoku");

        if (data is not null)
        {
            Puzzle.ViewModel.LoadXml(data, isFileBacked: false); 
        }
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

    public double GetHeaderHeight() => Header.Height;
}
