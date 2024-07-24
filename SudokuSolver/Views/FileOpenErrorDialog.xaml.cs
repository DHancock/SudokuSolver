using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class FileOpenErrorDialog : ContentDialog
{
    public FileOpenErrorDialog(XamlRoot xamlRoot, ElementTheme actualTheme)
    {
        this.InitializeComponent();

        Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];

        XamlRoot = xamlRoot;
        RequestedTheme = actualTheme;

        Title = App.Instance.AppDisplayName;
        PrimaryButtonText = App.Instance.ResourceLoader.GetString("OKButton");
        DefaultButton = ContentDialogButton.Primary;

        Loaded += (s, e) => Utils.PlayExclamation();
    }

    public void AddError(string fileName, string details)
    {
        if (IsLoaded)
        {
            AddErrorInternal(fileName, details);
        }
        else
        {
            Loaded += (s, e) => AddErrorInternal(fileName, details);
        }

        void AddErrorInternal(string fileName, string details)
        {
            TreeViewNode child = new TreeViewNode();
            child.Content = details;

            TreeViewNode parent = new TreeViewNode();
            parent.Content = fileName;
            parent.Children.Add(child);

            ErrorTreeView.RootNodes.Add(parent);
        }
    }
}