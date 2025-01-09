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
        invalidFileNameChars.AsSpan().Sort();

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
            ReadOnlySpan<char> invalidChars = invalidFileNameChars.AsSpan();

            foreach (char c in args.NewText)
            {
                if (invalidChars.BinarySearch(c) >= 0)
                {
                    args.Cancel = true;
                    break;
                }
            }
        }
    }

    private void NewTabNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(NewTabNameTextBox.Text);
    }

    public string NewName => NewTabNameTextBox.Text.Trim();

    private async void NewTabNameTextBox_Paste(object sender, TextControlPasteEventArgs e)
    {
        e.Handled = true; // disable the default paste action

        DataPackageView view = Clipboard.GetContent();

        if (view.Contains(StandardDataFormats.Text))
        {
            try
            {
                string pasteText = await view.GetTextAsync();
                pasteText = FilterInvalidChars(pasteText);

                if (pasteText.Length > 0) // mirrors behavior when renaming files in Explorer
                {
                    TextBox nameTextBox = (TextBox)sender;

                    string left = nameTextBox.Text.Substring(0, nameTextBox.SelectionStart);
                    string right = nameTextBox.Text.Substring(nameTextBox.SelectionStart + nameTextBox.SelectionLength);

                    nameTextBox.Text = left + pasteText + right;
                    nameTextBox.SelectionStart = left.Length + pasteText.Length;

                    if (nameTextBox.Text.Length > nameTextBox.MaxLength)
                    {
                        int caretPos = Math.Min(nameTextBox.SelectionStart, nameTextBox.MaxLength);

                        nameTextBox.Text = nameTextBox.Text.Substring(0, nameTextBox.MaxLength);
                        nameTextBox.SelectionStart = caretPos;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }

    private string FilterInvalidChars(string source)
    {
        StringBuilder sb = new StringBuilder(source.Length);
        ReadOnlySpan<char> invalidChars = invalidFileNameChars.AsSpan();

        foreach (char c in source)
        {
            if (invalidChars.BinarySearch(c) < 0)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
