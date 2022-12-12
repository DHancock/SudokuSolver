using Microsoft.UI.Xaml.Media;

using Sudoku.ViewModels;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    private PuzzleViewModel? viewModel;
    public bool IsPrintView { get; set; } = false;

    public PuzzleView()
    {
        InitializeComponent();
    
        Loaded += async (s, e) =>
        {
            // switch on the brush transition animation only for user initiated theme
            // changes, not when the window is opened to avoid ui flashing.
            await Task.Delay(250);
            PuzzleBrushTransition.Duration = new TimeSpan(0, 0, 0, 0, 250);
        };

        SizeChanged += (s, e) =>
        {
            // printers generally have much higher resolutions than monitors
            if (!IsPrintView)
                Grid.AdaptForScaleFactor(e.NewSize.Width);
        };
    }
     
    public PuzzleViewModel? ViewModel
    {
        get => viewModel;

        set
        {
            Debug.Assert(value is not null);
            DataContext = viewModel = value;
        }
    }
}
