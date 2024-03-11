using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsTabViewItem : TabViewItem
{
    public SettingsViewModel ViewModel { get; } = SettingsViewModel.Data;
    private RelayCommand CloseOtherTabsCommand { get; }
    private RelayCommand CloseLeftTabsCommand { get; }
    private RelayCommand CloseRightTabsCommand { get; }

    private bool isHorizontal;


    public SettingsTabViewItem()
    {
        this.InitializeComponent();

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

    public SettingsTabViewItem(SettingsTabViewItem source) : this()
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
                {
                    LayoutRoot.ColumnDefinitions.RemoveAt(LayoutRoot.ColumnDefinitions.Count - 1);
                }
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
        MainWindow window = App.Instance.GetWindowForElement(this);
        window.CloseTab(this);
    }

    private bool CanCloseOtherTabs(object? param = null)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        return window.CanCloseOtherTabs();
    }

    private async void ExecuteCloseOtherTabs(object? param)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        await window.ExecuteCloseOtherTabs();
    }

    private bool CanCloseLeftTabs(object? param = null)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        return window.CanCloseLeftTabs();
    }

    private async void ExecuteCloseLeftTabs(object? param)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        await window.ExecuteCloseLeftTabs();
    }

    private bool CanCloseRightTabs(object? param = null)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        return window.CanCloseRightTabs();
    }

    private async void ExecuteCloseRightTabs(object? param)
    {
        MainWindow window = App.Instance.GetWindowForElement(this);
        await window.ExecuteCloseRightTabs();
    }

    public void UpdateContextMenuItemsEnabledState()
    {
        CloseOtherTabsCommand.RaiseCanExecuteChanged();
        CloseLeftTabsCommand.RaiseCanExecuteChanged();
        CloseRightTabsCommand.RaiseCanExecuteChanged();
    }
}
