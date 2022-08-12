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
    private readonly PrintDocument printDocument;
    private readonly IPrintDocumentSource printDocumentSource;

    private FrameworkElement? currentView;
    private Canvas? printCanvas;

    public PrintHelper(IntPtr hWnd, XamlRoot xamlRoot)
    {
        this.hWnd = hWnd;
        this.xamlRoot = xamlRoot;

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        printManager = PrintManagerInterop.GetForWindow(hWnd);
        printManager.PrintTaskRequested += PrintTaskRequested;

        printDocument = new PrintDocument();
        printDocument.Paginate += Paginate;
        printDocument.GetPreviewPage += GetPreviewPage;
        printDocument.AddPages += AddPages;

        // if a local copy of the document source isn't used a ComException is thrown
        // marshalled on a different thread error (RPC_E_WRONG_THREAD)
        printDocumentSource = printDocument.DocumentSource;
    }

    public static bool IsSupported => PrintManager.IsSupported();  

    public async void PrintView(FrameworkElement view)
    {
        try
        {
            Debug.Assert(PrintManager.IsSupported());
            Debug.Assert(printCanvas == null);

            currentView = view;

            await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
        }
        catch (Exception ex)
        {
            Dialogs.ShowModalMessage("A printing error occured", ex.Message, xamlRoot);
        }
    }

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
    {
        PrintTask printTask = e.Request.CreatePrintTask("Print Sudoku Puzzle", (args) =>
        {
            args.SetSource(printDocumentSource);
        });

        printTask.Completed += (s, args) =>
        {
            // this is called after the data is copied to the os spooler, not actually printed
            // it will be called before the awaited print ui returns
            printCanvas = null;
            currentView = null;

            if (args.Completion == PrintTaskCompletion.Failed)
                dispatcherQueue.TryEnqueue(() => Dialogs.ShowModalMessage("A printing error occured", string.Empty, xamlRoot));
        };
    }

    private void Paginate(object sender, PaginateEventArgs e)
    {
        const double cPaddingPercentage = 10;
        Debug.Assert(currentView != null);

        // print a single page
        printDocument.SetPreviewPageCount(1, PreviewPageCountType.Final);

        // deterimine the page size
        PrintPageDescription pd = e.PrintTaskOptions.GetPageDescription(0);

        double inset = Math.Min(pd.ImageableRect.Height, pd.ImageableRect.Width) * (cPaddingPercentage / 100D);

        if (printCanvas == null)
        {
            printCanvas = new Canvas();
            printCanvas.Children.Add(currentView);
        }

        printCanvas.Width = pd.PageSize.Width;
        printCanvas.Height = pd.PageSize.Height;

        currentView.Width = pd.ImageableRect.Width - (inset * 2.0);
        currentView.Height = pd.ImageableRect.Height - (inset * 2.0);

        Canvas.SetLeft(currentView, pd.ImageableRect.Left + inset);
        Canvas.SetTop(currentView, pd.ImageableRect.Top + inset);
    }
    
    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        // the same canvas as the printed view
        printDocument.SetPreviewPage(e.PageNumber, printCanvas); 
    }

    private void AddPages(object sender, AddPagesEventArgs e)
    {
        printDocument.AddPage(printCanvas);
        printDocument.AddPagesComplete();
    }
}
