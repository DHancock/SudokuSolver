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
            };
        }

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
        closeLeftCommand = new RelayCommand(ExecuteCloseLeftTab, CanCloseLeftTabs);
        closeRightCommand = new RelayCommand(ExecuteCloseRightTab, CanCloseRightTabs);
    }



    // used for launch activation and new window commands
    public MainWindow(RectInt32 bounds, StorageFile? storageFile) : this(bounds)
    {
        if (storageFile is null)
            AddTab(CreatePuzzleTab());
        else
            AddTab(CreatePuzzleTab(storageFile));
    }


    // used when a tab is dragged and dropped outside of its parent window
    public MainWindow(TabViewItem newTab, RectInt32 bounds) : this(bounds)
    {
        if (newTab.Content is PuzzleTabContent)
            AddTab(CreatePuzzleTab(newTab));
        else
            AddTab(CreateSettingsTab());
    }

    private void FocusLastSelectedCell()
    {
        if (Tabs.SelectedItem is TabViewItem tvi && tvi.Content is PuzzleTabContent puzzleTab)
            puzzleTab.FocusLastSelectedCell();
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




    private async Task<bool> AttemptToCloseTabs(IList<object> tabs)
    {
        processingClose = true;

        List<(TabViewItem tab, int index)> modifiedTabs = new();
        List<object> unModifiedTabs = new();

        for (int index = 0; index < tabs.Count; index++)
        {
            if ((tabs[index] is TabViewItem tab) && (tab.Content is PuzzleTabContent puzzle) && puzzle.ViewModel.IsModified)
            {
                modifiedTabs.Add((tab, index));
            }
            else
            {
                unModifiedTabs.Add(tabs[index]);
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
            PuzzleTabContent puzzleTabContent = (PuzzleTabContent)tab.Content;

            bool closed = await puzzleTabContent.HandleTabCloseRequested();

            if (closed)
            {
                CloseTab(tab);
            }
            else
            {
                processingClose = false;
                puzzleTabContent.FocusLastSelectedCell();
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

    public void AddOrSelectSettingsTab(int index = -1)
    {
        if (Tabs.TabItems.FirstOrDefault(x => (x is TabViewItem tvi && tvi.Content is SettingsTabContent)) is not TabViewItem settingsTab)
        {
            settingsTab = CreateSettingsTab();

            if ((index < 0) || (index >= Tabs.TabItems.Count))
            {
                Tabs.TabItems.Add(settingsTab);
            }
            else
            {
                Tabs.TabItems.Insert(index, settingsTab);
            }

            AddDragRegionEventHandlers(settingsTab);
        }

        Tabs.SelectedItem = settingsTab;
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


    public TabViewItem CreatePuzzleTab()
    {
        return new TabViewItem()
        {
            Header = App.cNewPuzzleName,
            Content = new PuzzleTabContent(),
            ContextFlyout = CreateTabHeaderContextFlyout(),
        };
    }

    public TabViewItem CreatePuzzleTab(StorageFile storagefile)
    {
        return new TabViewItem()
        {
            Header = storagefile.Name,
            Content = new PuzzleTabContent(storagefile),
            ContextFlyout = CreateTabHeaderContextFlyout(),
        };
    }

    // cannot resue the tab directly because it may already have a different XamlRoot
    public TabViewItem CreatePuzzleTab(TabViewItem source)
    {
        Debug.Assert(source.Header is string);
        Debug.Assert(source.Content is PuzzleTabContent);
        
        PuzzleTabContent sourcecontent = (PuzzleTabContent)source.Content;

        return new TabViewItem()
        {
            Header = source.Header,
            IconSource = sourcecontent.IsModified ? new SymbolIconSource() { Symbol = Symbol.Edit, } : null,
            Content = new PuzzleTabContent(sourcecontent),
            ContextFlyout = CreateTabHeaderContextFlyout(),
        };
    }

    private TabViewItem CreateSettingsTab()
    {
        return new TabViewItem()
        {
            Header = "Settings",
            IconSource = new SymbolIconSource() { Symbol = Symbol.Setting, },
            Content = new SettingsTabContent(),
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
        item.KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.C, });
       
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
            Tabs.SelectedItem = tab;

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
        App.Instance.CreateWindow(args.Tab, bounds);  
    }

    private void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        args.Data.Properties.Add(cDataIdentifier, args.Tab);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void Tabs_TabStripDrop(object sender, DragEventArgs e)
    {
        // This event is called when we're dragging between different TabViews

        if (e.DataView.Properties.TryGetValue(cDataIdentifier, out object? obj))
        {
            if ((obj is TabViewItem sourceTabViewItem) &&
                (sender is TabView destinationTabView) &&
                (sourceTabViewItem.Parent is TabViewListView sourceTabViewListView))
            {
                // First we need to get the position in the List to drop to
                int index = -1;

                // Determine which items in the list our pointer is between.
                for (int i = 0; i < destinationTabView.TabItems.Count; i++)
                {
                    if (destinationTabView.ContainerFromIndex(i) is TabViewItem item)
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

                if (sourceTabViewItem.Content is PuzzleTabContent)
                {
                    TabViewItem newTab = CreatePuzzleTab(sourceTabViewItem);

                    if ((index < 0) || (index >= destinationTabView.TabItems.Count))
                    {
                        destinationTabView.TabItems.Add(newTab);
                    }
                    else
                    {
                        destinationTabView.TabItems.Insert(index, newTab);
                    }

                    AddDragRegionEventHandlers(newTab);
                    destinationTabView.SelectedItem = newTab;
                }
                else
                {
                    AddOrSelectSettingsTab(index);
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
        if (tab.Content is PuzzleTabContent puzzleTabContent)
        {
            if (await puzzleTabContent.HandleTabCloseRequested())
            {
                CloseTab(tab);
            }
        }
        else
        {
            Debug.Assert(tab.Content is SettingsTabContent);
            CloseTab(tab);
        }
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((e.AddedItems.Count == 1) && (e.RemovedItems.Count == 1))
        {
            Debug.Assert(e.RemovedItems[0] is TabViewItem);
            Debug.Assert(e.AddedItems[0] is TabViewItem);

            bool lastIsSettings = ((TabViewItem)e.RemovedItems[0]).Content is SettingsTabContent;
            bool newIsSettings = ((TabViewItem)e.AddedItems[0]).Content is SettingsTabContent;

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
        Tabs_AddTabButtonClick(Tabs, args);
    }

    private async void CloseSelectedTab_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if ((Tabs.SelectedItem is TabViewItem tab) && tab.IsClosable)
        {
            await TabCloseRequested(tab);
        }
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

    private bool CanCloseOtherTabs(object? param)
    {
        return Tabs.TabItems.Count > 1;
    }

    private async void ExecuteCloseOtherTabs(object? param)
    {
        List<object> otherTabs = new List<object>(Tabs.TabItems.Where(x => !x.Equals(Tabs.SelectedItem)));

        await AttemptToCloseTabs(otherTabs);
    }

    private bool CanCloseLeftTabs(object? param)
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[0] != Tabs.SelectedItem);
    }

    private async void ExecuteCloseLeftTab(object? param)
    {
        List<object> leftTabs = new List<object>();

        int selectedIndex = Tabs.TabItems.IndexOf(Tabs.SelectedItem);

        for (int index = 0; index < selectedIndex; index++)
        {
            leftTabs.Add(Tabs.TabItems[index]);
        }

        await AttemptToCloseTabs(leftTabs);
    }

    private bool CanCloseRightTabs(object? param)
    {
        return (Tabs.TabItems.Count > 1) && (Tabs.TabItems[Tabs.TabItems.Count - 1] != Tabs.SelectedItem);
    }

    private async void ExecuteCloseRightTab(object? param)
    {
        List<object> rightTabs = new List<object>();

        for (int index = Tabs.TabItems.IndexOf(Tabs.SelectedItem); index >= 0 && index < Tabs.TabItems.Count; index++)
        {
            rightTabs.Add(Tabs.TabItems[index]);
        }

        await AttemptToCloseTabs(rightTabs);
    }
}
