using SudokuSolver.Utilities;

namespace SudokuSolver.Views
{
    internal sealed class ErrorDialog : ContentDialog
    {
        public string? Message { set; private get; }
        public string? Details { set; private get; }

        public ErrorDialog(XamlRoot xamlRoot) : base()
        {
            XamlRoot = xamlRoot;
            Title = App.cDisplayName;
            PrimaryButtonText = "OK";

            Loaded += (s, e) =>
            {
                Utils.PlayExclamation();
                string content = string.Empty;

                if (!string.IsNullOrEmpty(Message))
                {
                    content = Message;

                    if (!string.IsNullOrEmpty(Details))
                        content += $"{Environment.NewLine}{Environment.NewLine}{Details}";
                }
                else if (!string.IsNullOrEmpty(Details))
                    content = Details;

                Content = content;
            };
        }
    }
}
