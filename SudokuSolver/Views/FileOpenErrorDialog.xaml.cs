using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class FileOpenErrorDialog : ContentDialog
{
    public ObservableCollection<ErrorInfo> Errors { get; } = new();

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

    public void AddError(string fileName, string details) => Errors.Add(new ErrorInfo(fileName, details));

    public static FileOpenErrorDialog Factory(FrameworkElement parent, string message, string details)
    {
        return new FileOpenErrorDialog(parent, message, details);
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

