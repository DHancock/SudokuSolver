// based largely on the following:
// https://github.com/marb2000/PrintSample/blob/master/MainWindow.xaml.cs
// https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/Printing/cs/PrintHelper.cs

namespace Sudoku.Utils;

internal sealed class PrintHelper
{
    private readonly IntPtr hWnd;
    private readonly XamlRoot xamlRoot;
    private readonly DispatcherQueue dispatcherQueue;
    private readonly PrintManager printManager;
    private readonly PrintDocument printDoc;
    private readonly IPrintDocumentSource printDocSource;

    private Control? currentView;

    public PrintHelper(IntPtr hWnd, XamlRoot xamlRoot)
    {
        this.hWnd = hWnd;
        this.xamlRoot = xamlRoot;

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        printManager = PrintManagerInterop.GetForWindow(hWnd);
        printManager.PrintTaskRequested += PrintTaskRequested;

        // Build a PrintDocument and register for callbacks
        printDoc = new PrintDocument();
        printDocSource = printDoc.DocumentSource;
        printDoc.Paginate += Paginate;
        printDoc.GetPreviewPage += GetPreviewPage;
        printDoc.AddPages += AddPages;
    }


    public static bool IsSupported => PrintManager.IsSupported();  


    public async void PrintView(Control view)
    {
        try
        {
            Debug.Assert(PrintManager.IsSupported());
            Debug.Assert(view is not null);

            currentView = view;
            await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
        }
        catch
        {
            // Printing cannot proceed at this time
            ContentDialog noPrintingDialog = new ContentDialog()
            {
                XamlRoot = xamlRoot,
                Title = "Printing error",
                Content = "\nSorry, printing can' t proceed at this time.",
                PrimaryButtonText = "OK"
            };

            await noPrintingDialog.ShowAsync();
        }
    }


    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        // Create the PrintTask.
        // Defines the title and delegate for PrintTaskSourceRequested
        var printTask = args.Request.CreatePrintTask("Print", PrintTaskSourceRequrested);

        // Handle PrintTask.Completed to catch failed print jobs
        printTask.Completed += PrintTaskCompleted;
    }

    private void PrintTaskSourceRequrested(PrintTaskSourceRequestedArgs args)
    {
        // Set the document source.
        args.SetSource(printDocSource);
    }




    private void Paginate(object sender, PaginateEventArgs e)
    {
        const double cPaddingPercentage = 10;

        if (currentView is not null)
        {
            // always print a single page
            printDoc.SetPreviewPageCount(1, PreviewPageCountType.Final);

            // deterimine the page size
            PrintPageDescription pd = e.PrintTaskOptions.GetPageDescription(0);
            currentView.Padding = new Thickness(Math.Min(pd.PageSize.Height, pd.PageSize.Width) * (cPaddingPercentage / 100D));
        }
    }

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        try
        {
            // the same print view
            printDoc.SetPreviewPage(e.PageNumber, currentView);
        }
        catch
        {
        }
    }


    private void AddPages(object sender, AddPagesEventArgs e)
    {
        printDoc.AddPage(currentView);
        printDoc.AddPagesComplete();
    }


    private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
    {
        if (args.Completion == PrintTaskCompletion.Failed)
        {
            dispatcherQueue.TryEnqueue(async () =>
            {
                ContentDialog noPrintingDialog = new ContentDialog()
                {
                    XamlRoot = xamlRoot,
                    Title = "Printing error",
                    Content = "\nSorry, failed to print.",
                    PrimaryButtonText = "OK"
                };
                await noPrintingDialog.ShowAsync();
            });
        }
    }
}
