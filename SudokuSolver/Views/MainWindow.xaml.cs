﻿using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

using Windows.Foundation.Collections;


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
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            ExtendsContentIntoTitleBar = true;
            RightPaddingColumn.MinWidth = AppWindow.TitleBar.RightInset / scaleFactor;

            UpdateCaptionButtonsTheme(LayoutRoot.ActualTheme);

            LayoutRoot.ActualThemeChanged += (s, a) =>
            {
                UpdateCaptionButtonsTheme(s.ActualTheme);
                ResetPuzzleTabsOpacity();
            };
        }

        App.Instance.RegisterWindow(this);

        AppWindow.Closing += async (s, args) =>
        {
            args.Cancel = true;
            await HandleWindowCloseRequestedAsync();
        };

        // these two are used in the iconic window displayed when hovering over the app's icon in the task bar
        AppWindow.Title = App.cAppDisplayName;
        AppWindow.SetIcon("Resources\\app.ico");

        AppWindow.MoveAndResize(App.Instance.GetNewWindowPosition(this, bounds));

        // setting the presenter will also activate the window
        if (windowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = windowState;
        }

        Activated += (s, e) =>
        {
            IsActive = e.WindowActivationState != WindowActivationState.Deactivated;

            if (IsActive)
            {
                WindowIcon.Opacity = 1;

                if (Tabs.SelectedItem is PuzzleTabViewItem puzzleTab)
                {
                    puzzleTab.FocusLastSelectedCell();
                }
            }
            else
            {
                WindowIcon.Opacity = 0.25;
            }

            // only the current active window's hotkeys should be active
            if (Tabs.SelectedItem is not null)
            {
                ((ITabItem)Tabs.SelectedItem).EnableKeyboardAccelerators(enable: IsActive);
            };
        };
    }

    private async Task HandleWindowCloseRequestedAsync()
    {
        if (Settings.Instance.SaveSessionState && (App.Instance.SessionHelper.IsExit || (App.Instance.WindowCount == 1)))
        {
            App.Instance.SessionHelper.AddWindow(this);
            Tabs.TabItems.Clear();
        }
        else
        {
            await AttemptToCloseTabsAsync(Tabs.TabItems);
        }
    }

    private void ResetPuzzleTabsOpacity()
    {
        foreach (object obj in Tabs.TabItems)
        {
            if (!ReferenceEquals(obj, Tabs.SelectedItem) && (obj is PuzzleTabViewItem puzzleTab))
            {
                puzzleTab.ResetOpacityTransitionForThemeChange();
            }
        }
    }

    public async Task PrintPuzzleAsync(PuzzleTabViewItem tab)
    {
        printHelper ??= new PrintHelper(this);
        await printHelper.PrintViewAsync(PrintCanvas, tab);
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
                    puzzleTab.FocusLastSelectedCell();
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

    private async void Tabs_TabItemsChangedAsync(TabView sender, IVectorChangedEventArgs args)
    {
        if (sender.TabItems.Count == 0)
        {
            Settings.Instance.RestoreBounds = RestoreBounds;
            Settings.Instance.WindowState = WindowState;

            AppWindow.Hide();

            App.Instance.UnRegisterWindow(this);

            if (App.Instance.WindowCount == 0)
            {
                if (Settings.Instance.SaveSessionState)
                {
                    await Task.WhenAll(Settings.Instance.SaveAsync(), App.Instance.SessionHelper.SaveAsync());
                }
                else
                {
                    await Settings.Instance.SaveAsync();
                }
            }

            Close();
        }
        else
        {
            bool enable = (sender.TabItems.Count > 1) && !IntegrityLevel.IsElevated;

            sender.CanReorderTabs = enable;
            sender.CanDragTabs = enable;
        }
    }

    public void AddTab(TabViewItem tab, int index = -1)
    {
        if (Tabs.IsLoaded)
        {
            AddTabInternal(tab, index);
        }
        else
        {
            Tabs.Loaded += (s, e) => AddTabInternal(tab, index);
        }

        void AddTabInternal(TabViewItem tab, int index)
        {
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
            AddDragRegionEventHandlers(tab);
        }
    }

    public void CloseTab(TabViewItem tab)
    {
        bool found = Tabs.TabItems.Remove(tab);
        Debug.Assert(found);

        // the tab's keyboard accelerators would still
        // be active (until presumably it's garbage collected)
        ((ITabItem)tab).EnableKeyboardAccelerators(enable: false);
    }

    private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        PInvoke.GetCursorPos(out System.Drawing.Point p);
        RectInt32 bounds = new RectInt32(p.X, p.Y, RestoreBounds.Width, RestoreBounds.Height);

        // cannot just move the tab to a new window because MenuFlyoutItemBase requires
        // an XamlRoot which cannot be updated once set, have to replace the ui
        CloseTab(args.Tab);

        MainWindow window = new MainWindow(WindowState.Normal, bounds);

        if (args.Tab is PuzzleTabViewItem puzzleTab)
        {
            window.AddTab(new PuzzleTabViewItem(window, puzzleTab));
        }
        else if (args.Tab is SettingsTabViewItem settingsTab)
        {
            window.AddTab(new SettingsTabViewItem(window, settingsTab));
        }

        window.Activate();
        window.AttemptSwitchToForeground();
    }

#pragma warning disable CA1822 // Mark members as static
    private void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        args.Data.Properties.Add(cDataIdentifier, args.Tab);
        args.Data.Properties.Add(cProcessId, Environment.ProcessId);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }
#pragma warning restore CA1822 // Mark members as static

    private void Tabs_TabStripDrop(object sender, DragEventArgs e)
    {
        // called when dragging over TabViewItem headers
        if (e.DataView.Properties.TryGetValue(cDataIdentifier, out object? obj) && (obj is TabViewItem sourceTab))
        {
            // First we need to get the position in the List to drop to
            int index = -1;

            // Determine which items in the list our pointer is between.
            for (int i = 0; i < Tabs.TabItems.Count; i++)
            {
                if (Tabs.ContainerFromIndex(i) is TabViewItem item)
                {
                    if (e.GetPosition(item).X - item.ActualWidth < 0)
                    {
                        index = i;
                        break;
                    }
                }
            }

            // single instanced so no need to dispatch
            // if it's a different window the menu's XamlRoot cannot be updated 
            // so have to replace the ui keeping the view model and model parts
            MainWindow? window = App.Instance.GetWindowForElement(sourceTab);
            window?.CloseTab(sourceTab);

            if (sourceTab is PuzzleTabViewItem existingPuzzleTab)
            {
                AddTab(new PuzzleTabViewItem(this, existingPuzzleTab), index);
            }
            else if (sourceTab is SettingsTabViewItem existingSettingsTab)
            {
                // preserve the drop position and existing expander state
                if (Tabs.TabItems.FirstOrDefault(x => x is SettingsTabViewItem) is TabViewItem existing)
                {
                    CloseTab(existing);
                }

                AddTab(new SettingsTabViewItem(this, existingSettingsTab), index);
            }
        }
    }

    private void Tabs_TabStripDragOver(object sender, DragEventArgs e)
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
                puzzleTab.FocusLastSelectedCell();
            }
        }
        else
        {
            CloseTab(args.Tab);
        }
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count == 1)
        {
            ((ITabItem)e.RemovedItems[0]).EnableKeyboardAccelerators(enable: false);

            if (e.RemovedItems[0] is SettingsTabViewItem)
            {
                SetWindowDragRegionsInternal();
            }
        }

        if (e.AddedItems.Count == 1)
        {
            ((ITabItem)e.AddedItems[0]).EnableKeyboardAccelerators(enable: true);

            if (e.AddedItems[0] is SettingsTabViewItem)
            {
                SetWindowDragRegionsInternal();
            }
            else if (e.AddedItems[0] is PuzzleTabViewItem puzzle)
            {
                puzzle.FocusLastSelectedCell();
            }
        }
    }

    private void UpdateCaptionButtonsTheme(ElementTheme theme)
    {
        Debug.Assert(theme is not ElementTheme.Default);

        if (AppWindow is not null)  // may occur if the window is closed immediately after requesting a theme change
        {
            AppWindow.TitleBar.BackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            if (theme == ElementTheme.Light)
            {
                AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.Black;
                AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.LightGray;
                AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
            }
            else
            {
                AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
                AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.White;
                AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.DimGray;
                AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.DimGray;
            }
        }
    }

    private void RightPaddingColumn_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // this accommodates both window width changes and adding/removing tabs
        SetWindowDragRegions();
    }

    private void NewTab_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        Tabs_AddTabButtonClick(Tabs, new object());
        args.Handled = true;
    }

    private void NavigateToNumberedTab_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        int index = sender.Key - VirtualKey.Number1;

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

    public async Task ExecuteCloseOtherTabsAsync()
    {
        if (CanCloseOtherTabs())
        {
            List<object> otherTabs = new List<object>(Tabs.TabItems.Count - 1);
            otherTabs.AddRange(Tabs.TabItems.Where(x => !ReferenceEquals(x, Tabs.SelectedItem)));

            await AttemptToCloseTabsAsync(otherTabs);
        }
    }

    public bool CanCloseLeftTabs()
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[0] != Tabs.SelectedItem);
    }

    public async Task ExecuteCloseLeftTabsAsync()
    {
        if (CanCloseLeftTabs())
        {
            List<object> leftTabs = new List<object>();
            int selectedIndex = Tabs.TabItems.IndexOf(Tabs.SelectedItem);

            for (int index = 0; index < selectedIndex; index++)
            {
                leftTabs.Add(Tabs.TabItems[index]);
            }

            await AttemptToCloseTabsAsync(leftTabs);
        }
    }

    public bool CanCloseRightTabs()
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[Tabs.TabItems.Count - 1] != Tabs.SelectedItem);
    }

    public async Task ExecuteCloseRightTabsAsync()
    {
        if (CanCloseRightTabs())
        {
            int selectedIndex = Tabs.TabItems.IndexOf(Tabs.SelectedItem);

            if (selectedIndex >= 0)
            {
                int startIndex = selectedIndex + 1;
                List<object> rightTabs = new List<object>(Tabs.TabItems.Count - startIndex);

                for (int index = startIndex; index < Tabs.TabItems.Count; index++)
                {
                    rightTabs.Add(Tabs.TabItems[index]);
                }

                await AttemptToCloseTabsAsync(rightTabs);
            }
        }
    }

    private void Tabs_Loaded(object sender, RoutedEventArgs e)
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
                                return false;
                            }
                        }

                        int count = 0;

                        foreach (XElement child in root.Descendants("settings"))
                        {
                            if ((++count > 1) || !SettingsTabViewItem.ValidateSessionData(child))
                            {
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
        HideSystemMenu();
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
                Text = ((ITabItem)tab).HeaderText,
                Tag = tab,
                Padding = narrow,
                IsEnabled = !ReferenceEquals(Tabs.SelectedItem, tab),
            };

            if (menuItem.IsEnabled)
            {
                menuItem.Click += MenuItem_Click;
            }

            if ((tab is PuzzleTabViewItem puzzleTab) && puzzleTab.IsModified)
            {
                menuItem.Icon = new SymbolIcon(Symbol.Edit);
            }
            else if (tab is SettingsTabViewItem)
            {
                menuItem.Icon = new SymbolIcon(Symbol.Setting);
            }

            menuFlyout.Items.Add(menuItem);
        }

        return menuFlyout;


        void MenuFlyout_Closed(object? sender, object e)
        {
            if (Tabs.SelectedItem is PuzzleTabViewItem puzzleTabViewItem)
            {
                puzzleTabViewItem.FocusLastSelectedCell();
            }
        }

        void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tabs.SelectedItem = ((FrameworkElement)sender).Tag;
        }
    }

    private PuzzleTabViewItem? FindExistingTab(StorageFile file)
    {
        foreach (object tab in Tabs.TabItems)
        {
            if (tab is PuzzleTabViewItem puzzleTab &&
                puzzleTab.SourceFile is not null &&
                puzzleTab.SourceFile.IsEqual(file))
            {
                return puzzleTab;
            }
        }

        return null;
    }

    public bool IsOpenInExistingTab(StorageFile file)
    {
        return FindExistingTab(file) is not null;
    }

    public void SwitchToTab(StorageFile file)
    {
        TabViewItem? existingTab = FindExistingTab(file);

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

    public object SelectedTab
    {
        get => Tabs.SelectedItem;
        set => Tabs.SelectedItem = value;
    }
}