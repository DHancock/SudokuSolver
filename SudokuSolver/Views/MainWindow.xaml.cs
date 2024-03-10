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

    private readonly RelayCommand newTabCommand;
    private readonly RelayCommand closeTabCommand;
    private readonly RelayCommand closeOtherCommand;
    private readonly RelayCommand closeLeftCommand;
    private readonly RelayCommand closeRightCommand;

    private bool processingClose = false;
    private PrintHelper? printHelper;

    private MainWindow(RectInt32 bounds)
    {
        InitializeComponent();

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
        if (Settings.Data.WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = Settings.Data.WindowState;
        }

        Activated += (s, e) =>
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
            {
                WindowIcon.Opacity = 1;
                FocusLastSelectedCell();
            }
            else
            {
                WindowIcon.Opacity = 0.25;
            }
        };

        // the tab context menu command handlers
        newTabCommand = new RelayCommand(ExecuteNewTab);
        closeTabCommand = new RelayCommand(ExecuteCloseTab);
        closeOtherCommand = new RelayCommand(ExecuteCloseOtherTabs, CanCloseOtherTabs);
        closeLeftCommand = new RelayCommand(ExecuteCloseLeftTabs, CanCloseLeftTabs);
        closeRightCommand = new RelayCommand(ExecuteCloseRightTabs, CanCloseRightTabs);
    }



    // used for launch activation and new window commands
    public MainWindow(RectInt32 bounds, StorageFile? storageFile = null) : this(bounds)
    {
        if (storageFile is null)
        {
            AddTab(CreatePuzzleTab());
        }
        else
        {
            AddTab(CreatePuzzleTab(storageFile));
        }
    }


    // used when a tab is dragged and dropped outside of its parent window
    public MainWindow(TabViewItem existingTab, RectInt32 bounds) : this(bounds)
    {
        if (existingTab is PuzzleTabViewItem existingPuzzleTab)
        {
            AddTab(CreatePuzzleTab(existingPuzzleTab));
        }
        else if (existingTab is SettingsTabViewItem existingSettingsTab)
        {
            AddTab(CreateSettingsTab(existingSettingsTab));
        }
    }

    private void FocusLastSelectedCell()
    {
        if (Tabs.SelectedItem is PuzzleTabViewItem puzzleTab)
        {
            puzzleTab.FocusLastSelectedCell();
        }
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
        List<object> unModifiedTabs = new();

        for (int index = 0; index < tabs.Count; index++)
        {
            if ((tabs[index] is PuzzleTabViewItem puzzleTab) && puzzleTab.ViewModel.IsModified)
            {
                modifiedTabs.Add((puzzleTab, index));
            }
            else
            {
                unModifiedTabs.Add(tabs[index]);  // including a settings tab
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

        foreach (object tab in unModifiedTabs)
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
            AddTab(CreateSettingsTab());
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

            UpdateTabContextMenuItemsEnabledState();
        }
    }

    private void UpdateTabContextMenuItemsEnabledState()
    {
        closeOtherCommand.RaiseCanExecuteChanged();
        closeLeftCommand.RaiseCanExecuteChanged();
        closeRightCommand.RaiseCanExecuteChanged();
    }

    public PuzzleTabViewItem CreatePuzzleTab()
    {
        return new PuzzleTabViewItem()
        {
            Header = App.cNewPuzzleName,
            ContextFlyout = CreateTabHeaderContextFlyout(),
        };
    }

    public PuzzleTabViewItem CreatePuzzleTab(StorageFile storagefile)
    {
        return new PuzzleTabViewItem(storagefile)
        {
            Header = storagefile.Name,
            ContextFlyout = CreateTabHeaderContextFlyout(),
        };
    }

    // cannot resue the tab directly because it may already have a different XamlRoot
    public PuzzleTabViewItem CreatePuzzleTab(PuzzleTabViewItem source)
    {
        Debug.Assert(source.Header is string);

        return new PuzzleTabViewItem(source)
        {
            Header = source.Header,
            IconSource = source.IsModified ? new SymbolIconSource() { Symbol = Symbol.Edit, } : null,
            ContextFlyout = CreateTabHeaderContextFlyout(),
        };
    }

    private SettingsTabViewItem CreateSettingsTab()
    {
        return new SettingsTabViewItem()
        {
            Header = "Settings",
            IconSource = new SymbolIconSource() { Symbol = Symbol.Setting, },
            ContextFlyout = CreateTabHeaderContextFlyout(isForPuzzleTab: false),
        };
    }

    private SettingsTabViewItem CreateSettingsTab(SettingsTabViewItem source)
    {
        return new SettingsTabViewItem(source)
        {
            Header = "Settings",
            IconSource = new SymbolIconSource() { Symbol = Symbol.Setting, },
            ContextFlyout = CreateTabHeaderContextFlyout(isForPuzzleTab: false),
        };
    }


    private MenuFlyout CreateTabHeaderContextFlyout(bool isForPuzzleTab = true)
    {
        MenuFlyout menuFlyout = new MenuFlyout();
        MenuFlyoutItem item;

        if (isForPuzzleTab)
        {
            item = new MenuFlyoutItem() { Text = "New tab", Command = newTabCommand, AccessKey = "N", };
            item.KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.N, });
            
            menuFlyout.Items.Add(item);
            menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        item = new MenuFlyoutItem() { Text="Close tab", Command = closeTabCommand, AccessKey="C", };
        item.KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.W, });
       
        menuFlyout.Items.Add(item);
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Close other tabs", Command = closeOtherCommand, AccessKey = "O", });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Close tabs to the left", Command = closeLeftCommand, AccessKey = "L", });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Close tabs to the right", Command = closeRightCommand, AccessKey = "R", });

        menuFlyout.Opened += (s, e) => ClearWindowDragRegions();
        menuFlyout.Closed += (s, e) => SetWindowDragRegionsInternal();

        return menuFlyout;
    }

    public void AddTab(TabViewItem tab, bool select = true)
    {
        Tabs.TabItems.Add(tab);

        if (select)
        {
            Tabs.SelectedItem = tab;
        }

        AddDragRegionEventHandlers(tab);
    }

    public bool CloseTab(object tab)
    {
        return Tabs.TabItems.Remove(tab);
    }

    private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        PInvoke.GetCursorPos(out System.Drawing.Point p);
        RectInt32 bounds = new RectInt32(p.X, p.Y, RestoreBounds.Width, RestoreBounds.Height);

        CloseTab(args.Tab);
        MainWindow window = new MainWindow(args.Tab, bounds);
        window.AttemptSwitchToForeground();
    }

#pragma warning disable CA1822 // Mark members as static
    public void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        args.Data.Properties.Add(cDataIdentifier, args.Tab);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }
#pragma warning restore CA1822 // Mark members as static

    private void Tabs_TabStripDrop(object sender, DragEventArgs e)
    {
        // This event is called when we're dragging between different TabViews

        if (e.DataView.Properties.TryGetValue(cDataIdentifier, out object? obj))
        {
            if ((obj is TabViewItem sourceTabViewItem) && (sourceTabViewItem.Parent is TabViewListView sourceTabViewListView))
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

                // single instanced so shouldn't need to dispatch
                sourceTabViewListView.Items.Remove(sourceTabViewItem);

                TabViewItem? newTab = null;

                if (sourceTabViewItem is PuzzleTabViewItem existingPuzzleTab)
                {
                    newTab = CreatePuzzleTab(existingPuzzleTab);
                }
                else if (sourceTabViewItem is SettingsTabViewItem existingSettingsTab)
                {
                    // replace the existing, it preserves the expanders expanded state and reduces code paths
                    object? existing = Tabs.TabItems.FirstOrDefault(x => x is SettingsTabViewItem);

                    if (existing != null)
                    {
                        Tabs.TabItems.Remove(existing);
                    }

                    newTab = CreateSettingsTab(existingSettingsTab);  
                }

                if (newTab is not null)
                {
                    if ((index < 0) || (index >= Tabs.TabItems.Count))
                    {
                        Tabs.TabItems.Add(newTab);
                    }
                    else
                    {
                        Tabs.TabItems.Insert(index, newTab);
                    }

                    AddDragRegionEventHandlers(newTab);
                    Tabs.SelectedItem = newTab;
                }
            }
        }
    }

    private void Tabs_TabStripDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey(cDataIdentifier))
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }
    }

    private void Tabs_AddTabButtonClick(TabView sender, object args)
    {
        TabViewItem tab = CreatePuzzleTab();
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
            Debug.Assert(tab is SettingsTabViewItem);
            CloseTab(tab);
        }
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((e.AddedItems.Count == 1) && (e.RemovedItems.Count == 1))
        {
            bool lastIsSettings = e.RemovedItems[0] is SettingsTabViewItem;
            bool newIsSettings = e.AddedItems[0] is SettingsTabViewItem;

            if (lastIsSettings || newIsSettings)
            {
                SetWindowDragRegionsInternal();
            }
        }

        UpdateTabContextMenuItemsEnabledState();
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

    private void ExecuteNewTab(object? param)
    {
        Tabs_AddTabButtonClick(Tabs, new object());
    }

    private async void ExecuteCloseTab(object? param)
    {
        if ((Tabs.SelectedItem is TabViewItem tab) && tab.IsClosable)
        {
            await TabCloseRequested(tab);
        }
    }

    private bool CanCloseOtherTabs(object? param = null)
    {
        return Tabs.TabItems.Count > 1;
    }

    private async void ExecuteCloseOtherTabs(object? param)
    {
        if (CanCloseOtherTabs())
        {
            List<object> otherTabs = new List<object>(Tabs.TabItems.Count - 1);
            otherTabs.AddRange(Tabs.TabItems.Where(x => !ReferenceEquals(x, Tabs.SelectedItem)));

            await AttemptToCloseTabs(otherTabs);
        }
    }

    private bool CanCloseLeftTabs(object? param = null)
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[0] != Tabs.SelectedItem);
    }

    private async void ExecuteCloseLeftTabs(object? param)
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

    private bool CanCloseRightTabs(object? param = null)
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[Tabs.TabItems.Count - 1] != Tabs.SelectedItem);
    }

    private async void ExecuteCloseRightTabs(object? param)
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
