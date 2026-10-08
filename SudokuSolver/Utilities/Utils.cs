namespace SudokuSolver.Utilities;

internal static class Utils
{
    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }

    public static ElementTheme NormaliseTheme(ElementTheme theme)
    {
        if (theme == ElementTheme.Default)
        {
            return App.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
        }

        return theme;
    }

    public static Vector3 GetOffsetFromXamlRoot(UIElement e)
    {
        Vector3 offset = e.ActualOffset;

        // FrameworkElement.Parent is the logical parent
        DependencyObject? dependencyObject = VisualTreeHelper.GetParent(e);

        while (dependencyObject != null)
        {
            if (dependencyObject is UIElement uie)
            {
                offset += uie.ActualOffset;

                if (uie is ScrollView sv)
                {
                    offset.X -= (float)sv.HorizontalOffset;
                    offset.Y -= (float)sv.VerticalOffset;
                }
                else if (uie is ScrollViewer svr)
                {
                    offset.X -= (float)svr.HorizontalOffset;
                    offset.Y -= (float)svr.VerticalOffset;
                }
            }

            dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
        }

        return offset;
    }

    public static RectInt32 ScaledRect(in Vector3 location, in Vector2 size, in float scale)
    {
        Debug.Assert(location.X >= 0f);
        Debug.Assert(location.Y >= 0f);
        Debug.Assert(size.X >= 0f);
        Debug.Assert(size.Y >= 0f);

        return new RectInt32((int)MathF.FusedMultiplyAdd(location.X, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(location.Y, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(size.X, scale, 0.5f),
                             (int)MathF.FusedMultiplyAdd(size.Y, scale, 0.5f));
    }

    public static RectInt32 GetPassthroughRect(UIElement e, float topBounds = 0f)
    {
        Vector3 offset = GetOffsetFromXamlRoot(e);
        Vector2 visibleSize = e.ActualSize;

        if (offset.Y < topBounds) // may be clipped if it's above the top edge of the scroll viewer
        {
            visibleSize.Y = offset.Y + visibleSize.Y - topBounds;

            if (visibleSize.Y < 0.1) // it's scrolled up out of view
            {
                return default;
            }

            offset.Y = topBounds;
        }

        // ignore clipping when part or all of the element is below the window bottom, it can't be clicked anyway

        return ScaledRect(offset, visibleSize, (float)e.XamlRoot.RasterizationScale);
    }

    public static bool InvokeMenuItemForKeyboardAccelerator(IList<MenuFlyoutItemBase> menuItems, VirtualKeyModifiers modifiers, VirtualKey key)
    {
        foreach (MenuFlyoutItemBase mfib in menuItems)
        {
            if (mfib is MenuFlyoutSubItem subItem)
            {
                if (InvokeMenuItemForKeyboardAccelerator(subItem.Items, modifiers, key))
                {
                    return true;
                }
            }
            else if (mfib is MenuFlyoutItem mfi)
            {
                foreach (KeyboardAccelerator ka in mfib.KeyboardAccelerators)
                {
                    if (ka.IsEnabled && (ka.Modifiers == modifiers) && (ka.Key == key))
                    {
                        Debug.Assert(ka.ScopeOwner is null);

                        if (mfi.Command is not null)
                        {
                            // CanExecute() defines if the action is performed, not the menu item's enabled state
                            // The enabled state is only updated when the menu is shown 
                            if (mfi.Command.CanExecute(mfi.CommandParameter))   
                            {
                                mfi.Command.Execute(mfi.CommandParameter);
                                return true;
                            }
                        }
                        else if (mfi.IsEnabled)
                        {
                            // the menu item has a click event handler, it's enabled state would be adjusted in code when required
                            AutomationPeer? ap = FrameworkElementAutomationPeer.FromElement(mfi);
                            MenuFlyoutItemAutomationPeer? ip = ap?.GetPattern(PatternInterface.Invoke) as MenuFlyoutItemAutomationPeer;

                            ip?.Invoke();
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
