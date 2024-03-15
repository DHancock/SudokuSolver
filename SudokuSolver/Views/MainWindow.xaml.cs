using SudokuSolver.Utilities;
using SudokuSolver.ViewModels;

using Windows.Foundation.Collections;


namespace SudokuSolver.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : WindowBase
{
    private const string cDataIdentifier = App.cDisplayName;
    private const string cProcessId = "pId";

    private bool processingClose = false;
    private PrintHelper? printHelper;

    private MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(WindowState windowState, RectInt32 bounds) : this()
    {
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            ExtendsContentIntoTitleBar = true;
            RightPaddingColumn.MinWidth = AppWindow.TitleBar.RightInset / GetScaleFactor();

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
            await HandleWindowCloseRequested();
        };

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
            if (e.WindowActivationState != WindowActivationState.Deactivated)
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
        };
    }

    private async Task HandleWindowCloseRequested()
    {
        if (processingClose)  // a second close attempt will always succeed
        {
            Tabs.TabItems.Clear();
            return;
        }

        await AttemptToCloseTabs(Tabs.TabItems);
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

    public async Task PrintPuzzle(PuzzleTabViewItem tab)
    {
        printHelper ??= new PrintHelper(this);
        await printHelper.PrintViewAsync(PrintCanvas, tab);
    }

    private async Task<bool> AttemptToCloseTabs(IList<object> tabs)
    {
        processingClose = true;

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

            bool closed = await puzzleTab.HandleTabCloseRequested();

            if (closed)
            {
                CloseTab(tab);
            }
            else
            {
                processingClose = false;
                puzzleTab.FocusLastSelectedCell();
                return true;
            }
        }

        foreach (TabViewItem tab in unModifiedTabs)
        {
            CloseTab(tab);
        }

        processingClose = false;
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

    private async void Tabs_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
    {
        if (sender.TabItems.Count == 0)
        {
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.WindowState = WindowState;

            AppWindow.Hide();

            bool isLastWindow = App.Instance.UnRegisterWindow(this);

            if (isLastWindow)
            {
                await Settings.Data.Save();
            }

            Close();
        }
        else
        {
            if ((sender.TabItems.Count == 1) || IntegrityLevel.IsElevated)
            {
                sender.CanReorderTabs = false;
                sender.CanDragTabs = false;
            }
            else
            {
                sender.CanReorderTabs = true;
                sender.CanDragTabs = true;
            }
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
        ((ITabItem)tab).AdjustKeyboardAccelerators(enable: false);
    }

    private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        PInvoke.GetCursorPos(out System.Drawing.Point p);
        RectInt32 bounds = new RectInt32(p.X, p.Y, RestoreBounds.Width, RestoreBounds.Height);

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

        window.AttemptSwitchToForeground();
    }

#pragma warning disable CA1822 // Mark members as static
    public void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        args.Data.Properties.Add(cDataIdentifier, args.Tab);
        args.Data.Properties.Add(cProcessId, Environment.ProcessId);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }
#pragma warning restore CA1822 // Mark members as static

    private void Tabs_TabStripDrop(object sender, DragEventArgs e)
    {
        // This event is called when we're dragging from one window to another 
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
            MainWindow? window = App.Instance.GetWindowForElement(sourceTab);
            window?.CloseTab(sourceTab);

            if (sourceTab is PuzzleTabViewItem existingPuzzleTab)
            {
                AddTab(new PuzzleTabViewItem(this, existingPuzzleTab), index);
            }
            else if (sourceTab is SettingsTabViewItem existingSettingsTab)
            {
                // replace the existing, it preserves the drop position
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

    private async void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        await TabCloseRequested(args.Tab);
    }

    private async Task TabCloseRequested(TabViewItem tab)
    {
        if (tab is PuzzleTabViewItem puzzleTab)
        {
            if (await puzzleTab.HandleTabCloseRequested())
            {
                CloseTab(tab);
            }
        }
        else
        {
            CloseTab(tab);
        }
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count == 1)
        {
            ((ITabItem)e.RemovedItems[0]).AdjustKeyboardAccelerators(enable: false);

            if (e.RemovedItems[0] is SettingsTabViewItem)
            {
                SetWindowDragRegionsInternal();
            }
        }

        if (e.AddedItems.Count == 1)
        {
            ((ITabItem)e.AddedItems[0]).AdjustKeyboardAccelerators(enable: true);
            ((ITabItem)e.AddedItems[0]).UpdateContextMenuItemsEnabledState();

            if (e.AddedItems[0] is SettingsTabViewItem)
            {
                SetWindowDragRegionsInternal();
            }
            else if (e.AddedItems[0] is PuzzleTabViewItem puzzleTab)
            {
                puzzleTab.FocusLastSelectedCell();
            }
        }
    }

    private void UpdateCaptionButtonsTheme(ElementTheme theme)
    {
        Debug.Assert(theme is not ElementTheme.Default);

        if (AppWindow is not null)  // may occure if the window is closed immediately after requesting a theme change
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
                AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Gainsboro;
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
        // this accomodates both window width changes and adding/removing tabs
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

    public async Task ExecuteCloseOtherTabs()
    {
        if (CanCloseOtherTabs())
        {
            List<object> otherTabs = new List<object>(Tabs.TabItems.Count - 1);
            otherTabs.AddRange(Tabs.TabItems.Where(x => !ReferenceEquals(x, Tabs.SelectedItem)));

            await AttemptToCloseTabs(otherTabs);
        }
    }

    public bool CanCloseLeftTabs()
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[0] != Tabs.SelectedItem);
    }

    public async Task ExecuteCloseLeftTabs()
    {
        if (CanCloseLeftTabs())
        {
            List<object> leftTabs = new List<object>();
            int selectedIndex = Tabs.TabItems.IndexOf(Tabs.SelectedItem);

            for (int index = 0; index < selectedIndex; index++)
            {
                leftTabs.Add(Tabs.TabItems[index]);
            }

            await AttemptToCloseTabs(leftTabs);
        }
    }

    public bool CanCloseRightTabs()
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[Tabs.TabItems.Count - 1] != Tabs.SelectedItem);
    }

    public async Task ExecuteCloseRightTabs()
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

                await AttemptToCloseTabs(rightTabs);
            }
        }
    }

    private void Tabs_Loaded(object sender, RoutedEventArgs e)
    {
        TabView tv = (TabView)sender;

        TabViewListView? tvlv = tv.FindChild<TabViewListView>();
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

        HWND foreground = PInvoke.GetForegroundWindow();
        HWND target = (HWND)WindowPtr;

        if (target != foreground)
        {
            return PInvoke.SetForegroundWindow(target);
        }

        return true;
    }
}
