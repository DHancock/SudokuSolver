using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class FileOpenErrorDialog : ContentDialog
{
    private FileOpenErrorDialog(FrameworkElement parent, string message, string details)
    {
        this.InitializeComponent();

        Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];

        XamlRoot = parent.XamlRoot;
        RequestedTheme = parent.ActualTheme;

        Title = App.cAppDisplayName;
        PrimaryButtonText = App.Instance.ResourceLoader.GetString("OKButton");
        DefaultButton = ContentDialogButton.Primary;

         AddError(message, details);

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

    public static FileOpenErrorDialog Factory(FrameworkElement parent, string message, string details)
    {
        return new FileOpenErrorDialog(parent, message, details);
    }
}