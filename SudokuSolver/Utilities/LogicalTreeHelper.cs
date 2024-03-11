namespace SudokuSolver.Utilities;

internal static class LogicalTreeHelper
{
    public static IEnumerable<UIElement> GetChildren(UIElement parent)
    {
        if (parent is Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                yield return child;
            }
        }
        else if (parent is UserControl uc) // i.e. the tab contents
        {
            if (uc.Content is Panel ucPanel)
            {
                foreach (UIElement child in ucPanel.Children)
                {
                    yield return child;
                }
            }
            else if (uc.Content is not null)
            {
                yield return uc.Content;
            }
        }
        else if (parent is ContentControl contentControl)  // an Expander, TabViewItem
        {
            if (contentControl.Content is Panel ccPanel)
            {
                foreach (UIElement child in ccPanel.Children)
                {
                    yield return child;
                }
            }
            else if (contentControl.Content is UIElement uie)
            {
                yield return uie;
            }
        }
        else if (parent is MenuBar menuBar)
        {
            foreach (MenuBarItem child in menuBar.Items)
            {
                yield return child;
            }
        }
    }
}
