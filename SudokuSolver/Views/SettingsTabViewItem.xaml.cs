using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

internal sealed partial class SettingsTabViewItem : TabViewItem, ITabItem, ISession
{
    public SettingsViewModel ViewModel { get; } = SettingsViewModel.Instance;
    private RelayCommand CloseOtherTabsCommand { get; }
    private RelayCommand CloseLeftTabsCommand { get; }
    private RelayCommand CloseRightTabsCommand { get; }

    private readonly MainWindow parentWindow;

    public SettingsTabViewItem(MainWindow parent)
    {
        this.InitializeComponent();

        parentWindow = parent;

        LayoutRoot.SizeChanged += LayoutRoot_SizeChanged;
        Loaded += SettingsTabViewItem_Loaded;

        RootScrollViewer.ViewChanged += RootScrollViewer_ViewChanged;

        // size changed can also indicate that this tab has been selected and that it's content is now valid 
        ThemeExpander.SizeChanged += Expander_SizeChanged;
        ViewExpander.SizeChanged += Expander_SizeChanged;
        LightColorsExpander.SizeChanged += Expander_SizeChanged;
        DarkColorsExpander.SizeChanged += Expander_SizeChanged;
        SessionExpander.SizeChanged += Expander_SizeChanged;

        CloseOtherTabsCommand = new RelayCommand(ExecuteCloseOtherTabsAsync, CanCloseOtherTabs);
        CloseLeftTabsCommand = new RelayCommand(ExecuteCloseLeftTabsAsync, CanCloseLeftTabs);
        CloseRightTabsCommand = new RelayCommand(ExecuteCloseRightTabsAsync, CanCloseRightTabs);

        static void SettingsTabViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsTabViewItem tab = (SettingsTabViewItem)sender;

            tab.Loaded -= SettingsTabViewItem_Loaded;
            tab.AdjustLayout(tab.ActualSize.X);

            Button? closeButton = tab.FindChild<Button>("CloseButton");
            Debug.Assert(closeButton is not null);

            if (closeButton is not null)
            {
                string text = App.Instance.ResourceLoader.GetString("CloseTabToolTip");
                ToolTipService.SetToolTip(closeButton, text);
            }
        }
    }

    private void Expander_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        parentWindow.SetWindowDragRegions();
    }

    private void RootScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        parentWindow.SetWindowDragRegions();
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

    public void Closed()
    {
        LayoutRoot.SizeChanged -= LayoutRoot_SizeChanged;

        RootScrollViewer.ViewChanged -= RootScrollViewer_ViewChanged;
        ThemeExpander.SizeChanged -= Expander_SizeChanged;
        ViewExpander.SizeChanged -= Expander_SizeChanged;
        LightColorsExpander.SizeChanged -= Expander_SizeChanged;
        DarkColorsExpander.SizeChanged -= Expander_SizeChanged;
        SessionExpander.SizeChanged -= Expander_SizeChanged;
    }

    private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AdjustLayout(e.NewSize.Width);
        }
    }

    private void AdjustLayout(double width)
    {
        const double cThreshold = 870;

        if (width < cThreshold)  // goto vertical
        {
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
        else
        {
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

    private void CloseTabClickHandler(object sender, RoutedEventArgs e)
    {
        parentWindow.CloseTab(this);
    }

    private bool CanCloseOtherTabs(object? param)
    {
        return parentWindow.CanCloseOtherTabs();
    }

    private async void ExecuteCloseOtherTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseOtherTabsAsync(this);
    }

    private bool CanCloseLeftTabs(object? param)
    {
        return parentWindow.CanCloseLeftTabs(this);
    }

    private async void ExecuteCloseLeftTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseLeftTabsAsync(this);
    }

    private bool CanCloseRightTabs(object? param)
    {
        return parentWindow.CanCloseRightTabs(this);
    }

    private async void ExecuteCloseRightTabsAsync(object? param)
    {
        await parentWindow.ExecuteCloseRightTabsAsync(this);
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

    public string HeaderText => ((TextBlock)Header).Text;

    public void EnableAccessKeys(bool enable)
    {
        // no access keys to disable when a content dialog is shown
    }

    public void InvokeKeyboardAccelerator(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        Utils.InvokeMenuItemForKeyboardAccelerator(((MenuFlyout)ContextFlyout).Items, modifiers, key);
    }

    public int PassthroughCount => 7;

    public void AddPassthroughContent(in RectInt32[] rects)
    {
        Debug.Assert(rects.Length >= PassthroughCount);

        double topClip = 0.0;

        if (RootScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
        {
            topClip = Utils.GetOffsetFromXamlRoot(RootScrollViewer).Y;

            ScrollBar? vScrollBar = RootScrollViewer.FindChild<ScrollBar>("VerticalScrollBar");
            Debug.Assert(vScrollBar is not null);

            if (vScrollBar is not null)
            {
                rects[0] = Utils.GetPassthroughRect(vScrollBar);
            }
        }

        rects[1] = Utils.GetPassthroughRect(ThemeExpander, topClip);
        rects[2] = Utils.GetPassthroughRect(ViewExpander, topClip);
        rects[3] = Utils.GetPassthroughRect(LightColorsExpander, topClip);
        rects[4] = Utils.GetPassthroughRect(DarkColorsExpander, topClip);
        rects[5] = Utils.GetPassthroughRect(SessionExpander, topClip);
        rects[6] = Utils.GetPassthroughRect(AboutInfo.HyperlinkElement, topClip);
    }       
}
