using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsTabViewItem : TabViewItem, ITabItem, ISession
{
    public SettingsViewModel ViewModel { get; } = SettingsViewModel.Instance;
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

            Button? closeButton = this.FindChild<Button>("CloseButton");
            Debug.Assert(closeButton is not null);

            if (closeButton is not null)
            {
                string text = App.Instance.ResourceLoader.GetString("CloseTabToolTip");
                ToolTipService.SetToolTip(closeButton, text);
            }
        }

        CloseOtherTabsCommand = new RelayCommand(ExecuteCloseOtherTabsAsync, CanCloseOtherTabs);
        CloseLeftTabsCommand = new RelayCommand(ExecuteCloseLeftTabsAsync, CanCloseLeftTabs);
        CloseRightTabsCommand = new RelayCommand(ExecuteCloseRightTabsAsync, CanCloseRightTabs);
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
            SessionExpander.IsExpanded = source.SessionExpander.IsExpanded;
        }
    }

    public SettingsTabViewItem(MainWindow parent, XElement root) : this(parent)
    {
        Loaded += SettingsTabViewItem_Loaded;

        void SettingsTabViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SettingsTabViewItem_Loaded;

            XElement? data = root.Element("theme");
            ThemeExpander.IsExpanded = (data is not null) && (data.Value == "true");

            data = root.Element("view");
            ViewExpander.IsExpanded = (data is not null) && (data.Value == "true");

            data = root.Element("light");
            LightColorsExpander.IsExpanded = (data is not null) && (data.Value == "true");

            data = root.Element("dark");
            DarkColorsExpander.IsExpanded = (data is not null) && (data.Value == "true");

            data = root.Element("start");
            SessionExpander.IsExpanded = (data is not null) && (data.Value == "true");
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
                Grid.SetRow(AboutInfo, 5);
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

    private async void ExecuteCloseOtherTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseOtherTabsAsync();
    }

    private bool CanCloseLeftTabs(object? param = null)
    {
        return parentWindow.CanCloseLeftTabs();
    }

    private async void ExecuteCloseLeftTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseLeftTabsAsync();
    }

    private bool CanCloseRightTabs(object? param = null)
    {
        return parentWindow.CanCloseRightTabs();
    }

    private async void ExecuteCloseRightTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseRightTabsAsync();
    }

    public XElement GetSessionData()
    {
        XElement root = new XElement("settings", new XAttribute("version", 1));

        root.Add(new XElement("theme", ThemeExpander.IsExpanded));
        root.Add(new XElement("view", ViewExpander.IsExpanded));
        root.Add(new XElement("light", LightColorsExpander.IsExpanded));
        root.Add(new XElement("dark", DarkColorsExpander.IsExpanded));
        root.Add(new XElement("start", SessionExpander.IsExpanded));

        return root;
    }

    public static bool ValidateSessionData(XElement root)
    {
        try
        {
            if ((root.Name == "settings") && (root.Attribute("version") is XAttribute vs) && int.TryParse(vs.Value, out int version))
            {
                if (version == 1)
                {
                    string[] names = ["theme", "view", "light", "dark", "start"];

                    foreach (string name in names)
                    {
                        XElement? data = root.Element(name);

                        if (data is null || !bool.TryParse(data.Value, out _))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }
        catch(Exception ex) 
        { 
            Debug.WriteLine(ex); 
        }

        return false;
    }
}
