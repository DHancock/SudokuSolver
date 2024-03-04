using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsTabContent : UserControl
{
    public SettingsViewModel ViewModel { get; } = SettingsViewModel.Data;

    private bool isHorizontal;

    public SettingsTabContent()
    {
        this.InitializeComponent();

        SizeChanged += SettingsTabContent_SizeChanged;
        Loaded += SettingsTabContent_Loaded;
    }

    private void SettingsTabContent_Loaded(object sender, RoutedEventArgs e)
    {
        AdjustLayout(ActualSize.X, initialise: true);
        Loaded -= SettingsTabContent_Loaded;
    } 

    private void SettingsTabContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        AdjustLayout(e.NewSize.Width);
    }

    private void AdjustLayout(double width, bool initialise = false)
    {
        const double cThreshold = 870;

        if (width < cThreshold)  // goto vertical
        {
            if (initialise || isHorizontal)
            {
                isHorizontal = false;

                Grid.SetColumn(AboutInfo, 0);
                Grid.SetRow(AboutInfo, 4);
                Grid.SetRowSpan(AboutInfo, 1);

                LayoutRoot.ColumnDefinitions[0].MinWidth = 0;

                Thickness margin = LayoutRoot.Margin;
                margin.Right = 20;
                LayoutRoot.Margin = margin;

                if (LayoutRoot.ColumnDefinitions.Count > 1)
                    LayoutRoot.ColumnDefinitions.RemoveAt(LayoutRoot.ColumnDefinitions.Count - 1);
            }
        }
        else
        {
            if (initialise || !isHorizontal)
            {
                isHorizontal = true;

                Grid.SetColumn(AboutInfo, 1);
                Grid.SetRow(AboutInfo, 0);
                Grid.SetRowSpan(AboutInfo, 5);

                LayoutRoot.ColumnDefinitions[0].MinWidth = LayoutRoot.ColumnDefinitions[0].MaxWidth;

                Thickness margin = LayoutRoot.Margin;
                margin.Right = 0;
                LayoutRoot.Margin = margin;

                if (LayoutRoot.ColumnDefinitions.Count == 1)
                    LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }
        }
    }

    private void RadioButton_Light(object sender, RoutedEventArgs e)
    {
        ViewModel.Theme = ElementTheme.Light;
    }

    private void RadioButton_Dark(object sender, RoutedEventArgs e)
    {
        ViewModel.Theme = ElementTheme.Dark;
    }

    private void RadioButton_System(object sender, RoutedEventArgs e)
    {
        ViewModel.Theme = ElementTheme.Default;
    }
}