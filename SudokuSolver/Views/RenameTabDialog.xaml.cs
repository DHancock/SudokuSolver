namespace SudokuSolver.Views;

internal sealed partial class RenameTabDialog : ContentDialog
{
    private readonly char[] invalidFileNameChars;

    public RenameTabDialog(string existingName)
    {
        this.InitializeComponent();

        PrimaryButtonText = App.Instance.ResourceLoader.GetString("OKButton");
        DefaultButton = ContentDialogButton.Primary;

        SecondaryButtonText = App.Instance.ResourceLoader.GetString("CancelButton");

        invalidFileNameChars = Path.GetInvalidFileNameChars();
        Array.Sort(invalidFileNameChars);

        Loaded += (s, e) =>
        {
            NewTabNameTextBox.Text = existingName;
            NewTabNameTextBox.SelectAll();
        };
    }

    private void NewTabName_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        if (args.NewText.Length > 0)
        {
            args.Cancel = args.NewText.Any(c => Array.BinarySearch(invalidFileNameChars, c) >= 0);
        }
    }

    private void NewTabNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(NewTabNameTextBox.Text);
    }

    public string NewName => NewTabNameTextBox.Text.Trim();
}
