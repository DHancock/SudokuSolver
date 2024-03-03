using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsTabContent : UserControl
{
    public SettingsViewModel ViewModel { get; } = SettingsViewModel.Data;

    public SettingsTabContent()
    {
        this.InitializeComponent();
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