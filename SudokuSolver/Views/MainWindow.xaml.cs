using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : Window, ISession
{
    private const string cDataIdentifier = "SudokuSolverId";
    private const string cProcessId = "pId";

    public bool IsActive { get; private set; } = true;

    private PrintHelper? printHelper;
    private DateTime lastPointerTimeStamp;

    public MainWindow(WindowState windowState, RectInt32 bounds) : this()
    {
        ExtendsContentIntoTitleBar = true;

        RightPaddingColumn.MinWidth = AppWindow.TitleBar.RightInset / scaleFactor;

        UpdateTheme();
        UpdateCaptionButtonColours();

        LayoutRoot.ActualThemeChanged += LayoutRoot_ActualThemeChanged;
        LayoutRoot.ProcessKeyboardAccelerators += LayoutRoot_ProcessKeyboardAccelerators;

        App.Instance.RegisterWindow(this);

        AppWindow.Closing += AppWindow_ClosingAsync;

        // these two are used in the iconic window displayed when hovering over the app's icon in the task bar
        AppWindow.Title = App.cAppDisplayName;
        AppWindow.SetIcon("Resources\\app.ico");

        AppWindow.MoveAndResize(App.Instance.GetNewWindowPosition(this, bounds));

        // setting the presenter state may also activate the window if the state changes
        if (windowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = windowState;
        }

        Activated += MainWindow_Activated;

        // work around bogus XLS0519 errors caused by using x:Bind to a private static event handler
        Tabs.TabStripDragOver += Tabs_TabStripDragOver;
        Tabs.TabStripDrop += Tabs_TabStripDrop;
        Tabs.TabDragStarting += Tabs_TabDragStarting;
        Tabs.TabDragCompleted += Tabs_TabDragCompleted;
        Tabs.SelectionChanged += Tabs_SelectionChanged;
        Tabs.Loaded += Tabs_Loaded;
    }

    private void LayoutRoot_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        // The sdk's search for global keyboard accelerators is a bit challenged in WAS 1.8.0
        // Do it here instead, the accelerators are in known positions in the visual tree
        args.Handled = true;

        if (Tabs.SelectedItem is not null) // guard against a closed last tab
        {
            foreach (KeyboardAccelerator ka in Tabs.KeyboardAccelerators)
            {
                if (ka.IsEnabled && (ka.Modifiers == args.Modifiers) && (ka.Key == args.Key))
                {
                    if (ka.Key == VirtualKey.T)
                    {
                        Tabs_AddTabButtonClick(Tabs, EventArgs.Empty);
                    }
                    else
                    {
                        NavigateToNumberedTab(ka.Key);
                    }
                    return;
                }
            }

            ((ITabItem)Tabs.SelectedItem).InvokeKeyboardAccelerator(args.Modifiers, args.Key);
        }
    }

    private void LayoutRoot_ActualThemeChanged(FrameworkElement sender, object args)
    {
        ContentDialogHelper.ThemeChanged(sender.ActualTheme);
        UpdateCaptionButtonColours();
    }

    private async void AppWindow_ClosingAsync(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (Settings.Instance.SaveSessionState && (App.Instance.WindowCount == 1))
        {
            App.Instance.SaveState();
            Environment.Exit(0);
        }

        // the user may need to save the tab contents
        args.Cancel = true;
        await AttemptToCloseTabsAsync(Tabs.TabItems);
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        IsActive = args.WindowActivationState != WindowActivationState.Deactivated;

        if (IsActive)
        {
            WindowIcon.Opacity = 1;

            if (Tabs.SelectedItem is PuzzleTabViewItem puzzleTab)
            {
                puzzleTab.FocusSelectedCell();
            }
        }
        else
        {
            WindowIcon.Opacity = 0.25;
        }
    }
                                                        
    public void UpdateTheme()
    {
        LayoutRoot.RequestedTheme = Settings.Instance.Theme;
    }

    public PrintHelper PrintHelper
    {
        get
        {
            printHelper ??= new PrintHelper(this);
            return printHelper;
        }
    }

    public bool IsCurrentlyPrinting
    {
        // avoids allocating the PrintHelper just to enable it's menu item
        get => printHelper is not null && printHelper.IsCurrentlyPrinting;
    }

    private async Task<bool> AttemptToCloseTabsAsync(IList<object> tabs)
    {
        List<(TabViewItem tab, int index)> modifiedTabs = new();
        List<TabViewItem> unModifiedTabs = new();

        for (int index = 0; index < tabs.Count; index++)
        {
            if ((tabs[index] is PuzzleTabViewItem puzzleTab) && puzzleTab.ViewModel.IsModified)
            {
                modifiedTabs.Add((puzzleTab, index));
            }
            else if (tabs[index] is TabViewItem tab)
            {
                unModifiedTabs.Add(tab);  // including a settings tab
            }
        }

        if (modifiedTabs.Count > 0)
        {
            modifiedTabs.Sort((a, b) =>
            {
                if (a.tab.IsSelected) return -1;
                if (b.tab.IsSelected) return 1;

                return a.index - b.index;
            });

            foreach ((TabViewItem tab, _) in modifiedTabs)
            {
                Tabs.SelectedItem = tab;
                PuzzleTabViewItem puzzleTab = (PuzzleTabViewItem)tab;

                if (await puzzleTab.SaveTabContentsAsync())
                {
                    CloseTab(tab);
                }
                else
                {
                    puzzleTab.FocusSelectedCell();
                    return true;
                }
            }
        }

        foreach (TabViewItem tab in unModifiedTabs)
        {
            CloseTab(tab);
        }

        return false;
    }

    public void AddOrSelectSettingsTab()
    {
        object? tab = Tabs.TabItems.FirstOrDefault(x => x is SettingsTabViewItem);

        if (tab is null)
        {
            AddTab(new SettingsTabViewItem(this));
        }
        else
        {
            Tabs.SelectedItem = tab;
        }
    }

    private void Tabs_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
    {
        CollectionChange change = args.As<IVectorChangedEventArgs>().CollectionChange;

        if ((change == CollectionChange.ItemInserted) || (change == CollectionChange.ItemRemoved))
        {
            App.Instance.IsModified = true;

            if (sender.TabItems.Count == 0)
            {
                AppWindow.Hide();

                if (App.Instance.WindowCount == 1)
                {
                    App.Instance.SaveState();
                    Environment.Exit(0);
                }

                App.Instance.UnRegisterWindow(this);
                Close();
            }
            else
            {
                sender.CanReorderTabs = Tabs.TabItems.Count > 1;
                sender.CanDragTabs = true;
            }
        }
        else if (change == CollectionChange.Reset)
        {
            sender.CanDragTabs = !IntegrityLevel.IsElevated;
            sender.CanReorderTabs = !IntegrityLevel.IsElevated;
        }
    }

    public void AddTab(TabViewItem tab, int index = -1)
    {
        // if the TabView isn't loaded yet it will store the tabs temporarily
        // and then add them once it has. 
        Debug.Assert(tab is ITabItem);
        Debug.Assert(tab is ISession);

        if ((index >= 0) && (index < Tabs.TabItems.Count))
        {
            Tabs.TabItems.Insert(index, tab);
        }
        else
        {
            Tabs.TabItems.Add(tab);
        }

        Tabs.SelectedItem = tab;
    }

    public void CloseTab(TabViewItem tab)
    {
        bool found = Tabs.TabItems.Remove(tab);
        Debug.Assert(found);

        ((ITabItem)tab).Closed();
    }

    private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        if (PInvoke.GetCursorPos(out System.Drawing.Point p))
        {
            RectInt32 bounds = new RectInt32(p.X, p.Y, RestoreBounds.Width, RestoreBounds.Height);

            MainWindow? window = null;
            TabViewItem? tab = null;

            try
            {
                window = new MainWindow(WindowState.Normal, bounds);

                // cannot just move the tab to a new window because MenuFlyoutItemBase requires an XamlRoot
                // which cannot be updated once set, have to replace the ui

                if (args.Tab is PuzzleTabViewItem puzzleTab)
                {
                    tab = new PuzzleTabViewItem(window, puzzleTab);
                }
                else if (args.Tab is SettingsTabViewItem settingsTab)
                {
                    tab = new SettingsTabViewItem(window, settingsTab);
                }

                if (tab is not null)
                {
                    window.AddTab(tab);
                    window.Activate();
                    window.AttemptSwitchToForeground();

                    // close tab after creating the window otherwise the app could terminate with zero windows
                    CloseTab(args.Tab);
                }
            }
            catch (Exception ex) // not expected, it's just copying session data from one tab to another
            {
                Debug.WriteLine(ex.ToString());

                if (window is not null)
                {
                    window.AppWindow.Destroy();
                    App.Instance.UnRegisterWindow(window);
                }
            }
        }
    }

    private static void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        args.Data.Properties.Add(cDataIdentifier, args.Tab);
        args.Data.Properties.Add(cProcessId, Environment.ProcessId);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void Tabs_TabStripDrop(object sender, DragEventArgs args)
    {
        // called when:
        // a) dropping on to another window
        // b) dragging the only tab off and then dropping it back on to the same window
        // c) a duplicate of the above

        if (!args.Handled)
        {
            // guard against multiple calls with the same DragEventArgs, setting args.Handled won't stop them in itself
            args.Handled = true;

            if (args.DataView.Properties.TryGetValue(cDataIdentifier, out object? obj) && (obj is TabViewItem sourceTab))
            {
                // First we need to get the position in the List to drop to
                int index = -1;

                // Determine which items in the list our pointer is between.
                for (int i = 0; i < Tabs.TabItems.Count; i++)
                {
                    if (Tabs.ContainerFromIndex(i) is TabViewItem item)
                    {
                        if (args.GetPosition(item).X - item.ActualWidth < 0)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                MainWindow? window = App.Instance.GetWindowForElement(sourceTab);

                if (ReferenceEquals(window, this) && (Tabs.TabItems.Count == 1))
                {
                    return; // nothing to do
                }

                // It's from a different window, the tab has to be duplicated because a flyout's XamlRoot cannot be updated
                Debug.Assert(!ReferenceEquals(window, this));

                try
                {
                    if (sourceTab is PuzzleTabViewItem puzzleTab)
                    {
                        AddTab(new PuzzleTabViewItem(this, puzzleTab), index);
                    }
                    else if (sourceTab is SettingsTabViewItem settingsTab)
                    {
                        TabViewItem? existing = Tabs.TabItems.FirstOrDefault(x => x is SettingsTabViewItem) as TabViewItem;

                        // add first before closing an existing settings tab to avoid the window closing
                        AddTab(new SettingsTabViewItem(this, settingsTab), index);

                        if (existing is not null)
                        {
                            CloseTab(existing);
                        }
                    }

                    window?.CloseTab(sourceTab);
                }
                catch (Exception ex) // not expected, it's just copying session data from one tab to another
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
    }

    private static void Tabs_TabStripDragOver(object sender, DragEventArgs e)
    {
        try
        {
            if (e.DataView.Properties.ContainsKey(cDataIdentifier) &&
                (e.DataView.Properties.TryGetValue(cProcessId, out object value)) &&
                (Convert.ToInt32(value) == Environment.ProcessId))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private static void Tabs_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
    {
        if (args.Tab is not null) // it wasn't dragged off on to a different window
        {
            if (ReferenceEquals(sender.SelectedItem, args.Tab))
            {
                if (args.Tab is PuzzleTabViewItem puzzle)
                {
                    // there won't be a selection changed event if dragging the selected tab
                    puzzle.FocusSelectedCell();
                }
            }
            else   
            {
                sender.SelectedItem = args.Tab;
            }
        }
    }

    private void Tabs_AddTabButtonClick(TabView sender, object args)
    {
        TabViewItem tab = new PuzzleTabViewItem(this);
        AddTab(tab);
    }

    private async void Tabs_TabCloseRequestedAsync(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if ((args.Tab is PuzzleTabViewItem puzzleTab) && puzzleTab.IsModified)
        {
            if (await puzzleTab.SaveTabContentsAsync())
            {
                CloseTab(args.Tab);
            }
            else // user cancelled
            {
                puzzleTab.FocusSelectedCell();
            }
        }
        else
        {
            CloseTab(args.Tab);
        }
    }

    private static void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((e.AddedItems.Count == 1) &&  (e.AddedItems[0] is PuzzleTabViewItem puzzle))
        {
            puzzle.FocusSelectedCell();
        }
    }

    private void UpdateCaptionButtonColours()
    {
        if (AppWindow is not null)  // may occur if the window is closed immediately after requesting a theme change
        {
            AppWindow.TitleBar.BackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            if (LayoutRoot.ActualTheme == ElementTheme.Light)
            {
                AppWindow.TitleBar.ButtonForegroundColor = ContentDialogHelper.IsContentDialogOpen ? Colors.Gray : Colors.Black;
                AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.Black;
                AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.LightGray;
                AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                AppWindow.TitleBar.ButtonForegroundColor = ContentDialogHelper.IsContentDialogOpen ? Colors.Gray : Colors.White;
                AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.White;
                AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.DimGray;
                AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
        }
    }

    private void RightPaddingColumn_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // this accommodates both window width changes and adding/removing tabs
        SetWindowDragRegions();
    }

    private void NavigateToNumberedTab(VirtualKey key)
    {
        int index = key - VirtualKey.Number1;

        if (index == 8) // control 9 - always selects the last tab
        {
            index = Tabs.TabItems.Count - 1;
        }

        if ((index >= 0) && (index < Tabs.TabItems.Count))
        {
            Tabs.SelectedItem = Tabs.TabItems[index];
        }
    }

    public bool CanCloseOtherTabs()
    {
        return Tabs.TabItems.Count > 1;
    }

    public async Task ExecuteCloseOtherTabsAsync(TabViewItem sourceTab)
    {
        List<object> otherTabs = new List<object>(Tabs.TabItems.Count - 1);
        otherTabs.AddRange(Tabs.TabItems.Where(x => !ReferenceEquals(x, sourceTab)));

        await AttemptToCloseTabsAsync(otherTabs);
    }

    public bool CanCloseLeftTabs(TabViewItem sourceTab)
    {
        return (Tabs.TabItems.Count > 1) && !ReferenceEquals(Tabs.TabItems[0], sourceTab);
    }

    public async Task ExecuteCloseLeftTabsAsync(TabViewItem sourceTab)
    {
        List<object> leftTabs = new List<object>();
        int sourceIndex = Tabs.TabItems.IndexOf(sourceTab);

        for (int index = 0; index < sourceIndex; index++)
        {
            leftTabs.Add(Tabs.TabItems[index]);
        }

        await AttemptToCloseTabsAsync(leftTabs);
    }

    public bool CanCloseRightTabs(TabViewItem sourceTab)
    {
        return (Tabs.TabItems.Count > 1) && !ReferenceEquals(Tabs.TabItems[Tabs.TabItems.Count - 1], sourceTab);
    }

    public async Task ExecuteCloseRightTabsAsync(TabViewItem sourceTab)
    {
        int sourceIndex = Tabs.TabItems.IndexOf(sourceTab);

        if (sourceIndex >= 0)
        {
            int startIndex = sourceIndex + 1;
            List<object> rightTabs = new List<object>(Tabs.TabItems.Count - startIndex);

            for (int index = startIndex; index < Tabs.TabItems.Count; index++)
            {
                rightTabs.Add(Tabs.TabItems[index]);
            }

            await AttemptToCloseTabsAsync(rightTabs);
        }
    }

    private static void Tabs_Loaded(object sender, RoutedEventArgs e)
    {
        TabView tv = (TabView)sender;

        TabViewListView? tvlv = tv.FindChild<TabViewListView>("TabListView");
        Debug.Assert(tvlv is not null);

        // remove the delay displaying new tab headers
        tvlv?.ItemContainerTransitions.Clear();
    }

    internal bool AttemptSwitchToForeground()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        if (WindowHandle != PInvoke.GetForegroundWindow())
        {
            return PInvoke.SetForegroundWindow(WindowHandle);
        }

        return true;
    }

    public XElement GetSessionData()
    {
        XElement root = new XElement("window", new XAttribute("version", 1));

        if (Tabs.TabItems.Count > 0)
        {
            XElement bounds = new XElement("bounds");
            root.Add(bounds);

            RectInt32 restoreBounds = RestoreBounds;

            bounds.Add(new XElement(nameof(RectInt32.X), restoreBounds.X));
            bounds.Add(new XElement(nameof(RectInt32.Y), restoreBounds.Y));
            bounds.Add(new XElement(nameof(RectInt32.Width), restoreBounds.Width));
            bounds.Add(new XElement(nameof(RectInt32.Height), restoreBounds.Height));

            foreach (object tab in Tabs.TabItems)
            {
                root.Add(((ISession)tab).GetSessionData());
            }
        }

        return root;
    }

    public static bool ValidateSessionData(XElement root)
    {
        try
        {
            if ((root.Name == "window") && (root.Attribute("version") is XAttribute vw) && int.TryParse(vw.Value, out int version))
            {
                if (version == 1)
                {
                    XElement? bounds = root.Element("bounds");

                    if ((bounds is not null) && bounds.HasElements)
                    {
                        string[] names = [nameof(RectInt32.X), nameof(RectInt32.Y), nameof(RectInt32.Width), nameof(RectInt32.Height)];

                        foreach (string name in names)
                        {
                            XElement? field = bounds.Element(name);

                            if (field is null || !int.TryParse(field.Value, out int value))
                            {
                                return false;
                            }

                            if (((field.Name == nameof(RectInt32.Width)) || (field.Name == nameof(RectInt32.Height))) && (value < 0))
                            {
                                return false;
                            }
                        }

                        foreach (XElement child in root.Descendants("puzzle"))
                        {
                            if (!PuzzleTabViewItem.ValidateSessionData(child))
                            {
                                Debug.WriteLine("PuzzleTabViewItem.ValidateSessionData() failed");
                                return false;
                            }
                        }

                        int count = 0;

                        foreach (XElement child in root.Descendants("settings"))
                        {
                            if (++count > 1)
                            {
                                Debug.WriteLine("Settings session count failed");
                                return false;
                            }

                            if (!SettingsTabViewItem.ValidateSessionData(child))
                            {
                                Debug.WriteLine("SettingsTabViewItem.ValidateSessionData() failed");
                                return false;
                            }
                        }

                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return false;
    }

    private void TabStripHeader_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ShowSystemMenu(viaKeyboard: true); // open at keyboard location as not to obscure double clicks

        TimeSpan doubleClickTime = TimeSpan.FromMilliseconds(PInvoke.GetDoubleClickTime());
        DateTime utcNow = DateTime.UtcNow;

        if ((utcNow - lastPointerTimeStamp) < doubleClickTime)
        {
            PostCloseMessage();
        }
        else
        {
            lastPointerTimeStamp = utcNow;
        }
    }

    private void TabJumpListMenuButton_Click(object sender, RoutedEventArgs e)
    {
        MenuFlyout flyout = BuildTabJumpListMenu();
        FlyoutShowOptions options = new() { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };

        flyout.ShowAt((DependencyObject)sender, options);
    }

    private MenuFlyout BuildTabJumpListMenu()
    {
        const string cStyleKey = "DefaultMenuFlyoutPresenterStyle";
        const string cPaddingKey = "MenuFlyoutItemThemePaddingNarrow";

        MenuFlyout menuFlyout = new MenuFlyout()
        {
            XamlRoot = Content.XamlRoot,
            MenuFlyoutPresenterStyle = (Style)((FrameworkElement)Content).Resources[cStyleKey],
            OverlayInputPassThroughElement = Content,
        };

        menuFlyout.Closed += MenuFlyout_Closed;

        // ensure the use of narrow padding
        Thickness narrow = (Thickness)((FrameworkElement)Content).Resources[cPaddingKey];

        foreach (object tab in Tabs.TabItems)
        {
            MenuFlyoutItem menuItem = new()
            {
                Icon = new IconSourceElement() { IconSource = ((TabViewItem)tab).IconSource },
                Text = ((ITabItem)tab).HeaderText,
                Tag = tab,
                Padding = narrow,
                IsEnabled = !ReferenceEquals(Tabs.SelectedItem, tab),
            };

            if (menuItem.IsEnabled)
            {
                menuItem.Click += MenuItem_Click;
            }

            menuFlyout.Items.Add(menuItem);
        }

        return menuFlyout;


        void MenuFlyout_Closed(object? sender, object e)
        {
            if (Tabs.SelectedItem is PuzzleTabViewItem puzzleTabViewItem)
            {
                puzzleTabViewItem.FocusSelectedCell();
            }
        }

        void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tabs.SelectedItem = ((FrameworkElement)sender).Tag;
        }
    }

    private PuzzleTabViewItem? FindExistingTab(string filePath)
    {
        Debug.Assert(!string.IsNullOrEmpty(filePath));

        foreach (object tab in Tabs.TabItems)
        {
            if (tab is PuzzleTabViewItem puzzleTab &&
                filePath.Equals(puzzleTab.SourceFile, StringComparison.OrdinalIgnoreCase))
            {
                return puzzleTab;
            }
        }

        return null;
    }

    public bool IsOpenInExistingTab(string filePath)
    {
        return FindExistingTab(filePath) is not null;
    }

    public void SwitchToTab(string filePath)
    {
        TabViewItem? existingTab = FindExistingTab(filePath);

        if (existingTab is not null)
        {
            Tabs.SelectedItem = existingTab;
        }
    }

    private bool ContainsHeader(string newText)
    {
        return Tabs.TabItems.Any(x => ((ITabItem)x).HeaderText.Equals(newText, StringComparison.OrdinalIgnoreCase));
    }

    public string MakeUniqueHeaderText()
    {
        string basePart = App.Instance.ResourceLoader.GetString("Untitled");

        if (ContainsHeader(basePart))
        {
            string pattern;

            if (((FrameworkElement)Content).FlowDirection == FlowDirection.LeftToRight)
            {
                pattern = "{0} ({1})";
            }
            else
            {
                pattern = "({1}) {0}";
            }

            for (int count = 1; count < 25; count++)
            {
                string title = string.Format(pattern, basePart, count.ToString());

                if (!ContainsHeader(title))
                {
                    return title;
                }
            }
        }
       
        return basePart;
    }

    public int IndexOf(PuzzleTabViewItem tab) => Tabs.TabItems.IndexOf(tab);

    public object? SelectedTab
    {
        get => Tabs.SelectedItem;
        set => Tabs.SelectedItem = value;
    }
}