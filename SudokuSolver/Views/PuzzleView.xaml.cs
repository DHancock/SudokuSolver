using Sudoku.ViewModels;

namespace Sudoku.Views;

/// <summary>
/// Interaction logic for PuzzleView.xaml
/// </summary>
internal partial class PuzzleView : UserControl
{
    PuzzleViewModel? viewModel;

    public PuzzleView()
    {
        InitializeComponent();
     
        Loaded += async (s, e) => // yikes
        {
            await Task.Run(async () =>
            {
                await Task.Delay(50);

                bool success = this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    // switch on the brush transition animation only for user initiated theme
                    // changes, not when the window is opened to avoid ui flashing.
                    this.PuzzleBrushTransition.Duration = new TimeSpan(0, 0, 0, 0, 250);
                });

                Debug.Assert(success);
            });
        };
    }

    public PuzzleViewModel? ViewModel
    {
        get => viewModel;

        set
        {
            viewModel = value;
            DataContext = value;
        }
    }
}
