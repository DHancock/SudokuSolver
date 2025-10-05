namespace SudokuSolver;

internal sealed partial class ClipboardHelper
{
    private int currentValue = 0;

    public ClipboardHelper()
    {
        Clipboard_ContentChanged(null, EventArgs.Empty);

        Clipboard.ContentChanged += Clipboard_ContentChanged;
    }

    private async void Clipboard_ContentChanged(object? sender, object e)
    {
        try
        {
            DataPackageView dpv = Clipboard.GetContent();

            if (dpv.AvailableFormats.Contains(StandardDataFormats.Text))
            {
                string data = await dpv.GetTextAsync();

                if (int.TryParse(data, out int number) && (number > 0) && (number < 10))
                {
                    currentValue = number;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    public void Copy(int value)
    {
        DataPackage dp = new DataPackage();
        dp.SetText(value.ToString());
        Clipboard.SetContent(dp);

        currentValue = value;
    }

    public bool HasValue => currentValue > 0;

    public int Value => currentValue;
}
