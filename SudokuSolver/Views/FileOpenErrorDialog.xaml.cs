using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class FileOpenErrorDialog : ContentDialog
{
    public ObservableCollection<ErrorInfo> Errors { get; } = new();

    public FileOpenErrorDialog(FrameworkElement parent, string message, string details)
    {
        this.InitializeComponent();

        PrimaryButtonText = App.Instance.ResourceLoader.GetString("OKButton");
        DefaultButton = ContentDialogButton.Primary;

        AddError(message, details);

        Loaded += (s, e) => Utils.PlayExclamation();
    }

    public void AddError(string fileName, string details) => Errors.Add(new ErrorInfo(fileName, details));

    private void TreeView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            // the tree view would take focus, defeating the default button key handling
            e.Handled = true;
            Hide();
        }
    }
}

internal sealed class ErrorInfo
{
    public string Text { get; }
    public List<ErrorInfo> Children { get; }

    public ErrorInfo(string fileName, string details) 
    {
        Text = fileName;
        Children = new() { new ErrorInfo(details) };
    }

    public ErrorInfo(string details)
    {
        Text = details;
        Children = new();
    }

    public static FontWeight GetFontWeight(int childCount)
    {
        return childCount > 0 ? FontWeights.SemiBold : FontWeights.Normal;
    }
}

