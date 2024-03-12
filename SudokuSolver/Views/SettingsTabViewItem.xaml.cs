using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsTabViewItem : TabViewItem, ITabItem
{
    public SettingsViewModel ViewModel { get; } = SettingsViewModel.Data;
    private RelayCommand CloseOtherTabsCommand { get; }
    private RelayCommand CloseLeftTabsCommand { get; }
    private RelayCommand CloseRightTabsCommand { get; }

    private bool isOrientationHorizontal;
    private readonly MainWindow parentWindow;

    public SettingsTabViewItem(MainWindow parent)
    {
        this.InitializeComponent();

        parentWindow = parent;

        LayoutRoot.SizeChanged += LayoutRoot_SizeChanged;
        Loaded += SettingsTabViewItem_Loaded;

        void SettingsTabViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SettingsTabViewItem_Loaded;
            AdjustLayout(ActualSize.X, initialise: true);
        }

        CloseOtherTabsCommand = new RelayCommand(ExecuteCloseOtherTabs, CanCloseOtherTabs);
        CloseLeftTabsCommand = new RelayCommand(ExecuteCloseLeftTabs, CanCloseLeftTabs);
        CloseRightTabsCommand = new RelayCommand(ExecuteCloseRightTabs, CanCloseRightTabs);
    }

    public SettingsTabViewItem(MainWindow parent, SettingsTabViewItem source) : this(parent)
    {
        Loaded += SettingsTabViewItem_Loaded;

        void SettingsTabViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SettingsTabViewItem_Loaded;

            ThemeExpander.IsExpanded = source.ThemeExpander.IsExpanded;
            ViewExpander.IsExpanded = source.ViewExpander.IsExpanded;
            LightColorsExpander.IsExpanded = source.LightColorsExpander.IsExpanded;
            DarkColorsExpander.IsExpanded = source.DarkColorsExpander.IsExpanded;
        }
    }

    private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        AdjustLayout(e.NewSize.Width);
    }

    private void AdjustLayout(double width, bool initialise = false)
    {
        const double cThreshold = 870;

        if (width < cThreshold)  // goto vertical
        {
            if (initialise || isOrientationHorizontal)
            {
                isOrientationHorizontal = false;

                Grid.SetColumn(AboutInfo, 0);
                Grid.SetRow(AboutInfo, 4);
                Grid.SetRowSpan(AboutInfo, 1);

                LayoutRoot.ColumnDefinitions[0].MinWidth = 0;

                Thickness margin = LayoutRoot.Margin;
                margin.Right = 20;
                LayoutRoot.Margin = margin;

                if (LayoutRoot.ColumnDefinitions.Count > 1)
                {
                    LayoutRoot.ColumnDefinitions.RemoveAt(LayoutRoot.ColumnDefinitions.Count - 1);
                }
            }
        }
        else
        {
            if (initialise || !isOrientationHorizontal)
            {
                isOrientationHorizontal = true;

                Grid.SetColumn(AboutInfo, 1);
                Grid.SetRow(AboutInfo, 0);
                Grid.SetRowSpan(AboutInfo, 5);

                LayoutRoot.ColumnDefinitions[0].MinWidth = LayoutRoot.ColumnDefinitions[0].MaxWidth;

                Thickness margin = LayoutRoot.Margin;
                margin.Right = 0;
                LayoutRoot.Margin = margin;

                if (LayoutRoot.ColumnDefinitions.Count == 1)
                {
                    LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                }
            }
        }
    }

    private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        // always initialise the radio buttons state
        LightRadioButton.IsChecked = Settings.Data.Theme == ElementTheme.Light;
        DarkRadioButton.IsChecked = Settings.Data.Theme == ElementTheme.Dark;
        SystemRadioButton.IsChecked = Settings.Data.Theme == ElementTheme.Default;
    }

    public void AdjustKeyboardAccelerators(bool enable)
    {
        // accelerators on sub menus are only active when the menu is shown
        // which can only happen if this is the current selected tab
        if (ContextFlyout is MenuFlyout contextMenu)
        {
            foreach (MenuFlyoutItemBase mfib in contextMenu.Items)
            {
                foreach (KeyboardAccelerator ka in mfib.KeyboardAccelerators)
                {
                    ka.IsEnabled = enable;
                }
            }
        }
    }

    private void CloseTabClickHandler(object sender, RoutedEventArgs e)
    {
        parentWindow.CloseTab(this);
    }

    private bool CanCloseOtherTabs(object? param = null)
    {
        return parentWindow.CanCloseOtherTabs();
    }

    private async void ExecuteCloseOtherTabs(object? param)
    {
        await parentWindow.ExecuteCloseOtherTabs();
    }

    private bool CanCloseLeftTabs(object? param = null)
    {
        return parentWindow.CanCloseLeftTabs();
    }

    private async void ExecuteCloseLeftTabs(object? param)
    {
        await parentWindow.ExecuteCloseLeftTabs();
    }

    private bool CanCloseRightTabs(object? param = null)
    {
        return parentWindow.CanCloseRightTabs();
    }

    private async void ExecuteCloseRightTabs(object? param)
    {
        await parentWindow.ExecuteCloseRightTabs();
    }

    public void UpdateContextMenuItemsEnabledState()
    {
        CloseOtherTabsCommand.RaiseCanExecuteChanged();
        CloseLeftTabsCommand.RaiseCanExecuteChanged();
        CloseRightTabsCommand.RaiseCanExecuteChanged();
    }
}
