// based largely on the following:
// https://github.com/marb2000/PrintSample/blob/master/MainWindow.xaml.cs
// https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/Printing/cs/PrintHelper.cs

namespace Sudoku.Views;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

internal sealed class PrintHelper
{
    private readonly IntPtr hWnd;
    private readonly DispatcherQueue dispatcherQueue;
    private readonly PrintManager printManager;
    private readonly PrintDocument printDocument;
    private readonly IPrintDocumentSource printDocumentSource;

    private PrintTask? printTask;
    private PrintSize printSize = PrintSize.Size_80;   // TODO: save these in settings?
    private Alignment printAlignment = Alignment.MiddleCenter;   
    private FrameworkElement? currentView;
    private Canvas? printCanvas;
    private bool currentlyPrinting;
    private TaskCompletionSource? taskCompletionSource;

    public PrintHelper(IntPtr hWnd, DispatcherQueue dispatcherQueue)
    {
        this.hWnd = hWnd;
        this.dispatcherQueue = dispatcherQueue;

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

    public bool IsPrintingAvailable => PrintManager.IsSupported() && !currentlyPrinting;

    public async Task PrintViewAsync(FrameworkElement view)
    {
        try
        {
            Debug.Assert(IsPrintingAvailable);  // printing isn't reentrant
            Debug.Assert(printCanvas is null);

            currentlyPrinting = true;
            currentView = view;
            taskCompletionSource = new TaskCompletionSource();

            await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);

            // and wait for the print task to complete (from a different thread)
            await taskCompletionSource.Task;
        }
        finally
        {
            printCanvas = null;
            currentView = null;
            currentlyPrinting = false;
        }
    }   

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
    {
        printTask = e.Request.CreatePrintTask("Sudoku Puzzle", PrintTaskSourceRequestedHandler);
        
        printTask.Completed += (s, args) =>
        {
            // this is called after the data is handed off to whatever, not actually printed
            Debug.Assert(taskCompletionSource is not null);

            // notify the PrintViewAsync() function that the print task has completed
            if (args.Completion == PrintTaskCompletion.Failed)
                taskCompletionSource.SetException(new Exception(string.Empty));
            else
                taskCompletionSource.SetResult();
        };
    }

    private void PrintTaskSourceRequestedHandler(PrintTaskSourceRequestedArgs args)
    {
        Debug.Assert(printTask is not null);

        args.SetSource(printDocumentSource);

        PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
        IList<string> displayedOptions = printDetailedOptions.DisplayedOptions;

        // remove the colour or monochrome option from the default option set
        displayedOptions.Clear();
        displayedOptions.Add(StandardPrintTaskOptions.Copies);
        displayedOptions.Add(StandardPrintTaskOptions.Orientation);

        // add custom size option
        PrintCustomItemListOptionDetails sizeOption;
        sizeOption = printDetailedOptions.CreateItemListOption(CustomOptions.PrintSize.ToString(), "Puzzle size");
        sizeOption.AddItem(PrintSize.Size_100.ToString(), "Fit to page");
        sizeOption.AddItem(PrintSize.Size_90.ToString(), "90%");
        sizeOption.AddItem(PrintSize.Size_80.ToString(), "80%");
        sizeOption.AddItem(PrintSize.Size_70.ToString(), "70%");
        sizeOption.AddItem(PrintSize.Size_60.ToString(), "60%");
        sizeOption.AddItem(PrintSize.Size_50.ToString(), "50%");
        sizeOption.AddItem(PrintSize.Size_40.ToString(), "40%"); 
        sizeOption.AddItem(PrintSize.Size_30.ToString(), "30%");
        sizeOption.AddItem(PrintSize.Size_20.ToString(), "20%");
        sizeOption.AddItem(PrintSize.Size_10.ToString(), "10%");

        sizeOption.TrySetValue(printSize.ToString());
        displayedOptions.Add(sizeOption.OptionId);

        // add custom alignment option
        PrintCustomItemListOptionDetails alignOption;
        alignOption = printDetailedOptions.CreateItemListOption(CustomOptions.Alignment.ToString(), "Position on page");
        alignOption.AddItem(Alignment.TopLeft.ToString(), "top left");
        alignOption.AddItem(Alignment.TopCenter.ToString(), "top center");
        alignOption.AddItem(Alignment.TopRight.ToString(), "top right");
        alignOption.AddItem(Alignment.MiddleLeft.ToString(), "left of center");
        alignOption.AddItem(Alignment.MiddleCenter.ToString(), "centered");
        alignOption.AddItem(Alignment.MiddleRight.ToString(), "right of center");
        alignOption.AddItem(Alignment.BottomLeft.ToString(), "bottom left");
        alignOption.AddItem(Alignment.BottomCenter.ToString(), "bottom center");
        alignOption.AddItem(Alignment.BottomRight.ToString(), "bottom right");

        alignOption.TrySetValue(printAlignment.ToString());
        displayedOptions.Add(alignOption.OptionId);

        printDetailedOptions.OptionChanged += (sender, args) =>
        {
            bool invalidatePreview = false;
            string optionId = (string)args.OptionId;

            if (CustomOptions.PrintSize.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                printSize = Enum.Parse<PrintSize>((string)option.Value);
                invalidatePreview = true;
            }
            else if (CustomOptions.Alignment.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                printAlignment = Enum.Parse<Alignment>((string)option.Value);
                invalidatePreview = true;
            }

            if (invalidatePreview)
                dispatcherQueue.TryEnqueue(() => printDocument.InvalidatePreview());
        };
    }

    private enum PrintSize
    {
        Size_100 = 100,
        Size_90 = 90,
        Size_80 = 80,
        Size_70 = 70,
        Size_60 = 60,
        Size_50 = 50,
        Size_40 = 40,
        Size_30 = 30,
        Size_20 = 20,
        Size_10 = 10,
    }

    private enum Alignment
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    private enum CustomOptions
    {
        PrintSize,
        Alignment,
    }

    private void Paginate(object sender, PaginateEventArgs e)
    {
        Debug.Assert(currentView is not null);

        // print a single page
        printDocument.SetPreviewPageCount(1, PreviewPageCountType.Final);

        // deterimine the page size
        PrintPageDescription pd = e.PrintTaskOptions.GetPageDescription(0);

        if (printCanvas is null)
        {
            printCanvas = new Canvas();
            printCanvas.Children.Add(currentView);
        }

        printCanvas.Width = pd.PageSize.Width;
        printCanvas.Height = pd.PageSize.Height;

        double viewSize = Math.Min(pd.ImageableRect.Height, pd.ImageableRect.Width) * ((double)printSize / 100D);

        currentView.Width = viewSize;
        currentView.Height = viewSize;

        switch (printAlignment)
        {
            case Alignment.TopLeft:
                Canvas.SetLeft(currentView, pd.ImageableRect.Left);
                Canvas.SetTop(currentView, pd.ImageableRect.Top);
                break;

            case Alignment.TopCenter:
                Canvas.SetLeft(currentView, pd.ImageableRect.Left + (pd.ImageableRect.Width - currentView.Width) / 2);
                Canvas.SetTop(currentView, pd.ImageableRect.Top);
                break;

            case Alignment.TopRight:
                Canvas.SetLeft(currentView, pd.ImageableRect.Right - currentView.Width);
                Canvas.SetTop(currentView, pd.ImageableRect.Top);
                break;

            case Alignment.MiddleLeft:
                Canvas.SetLeft(currentView, pd.ImageableRect.Left);
                Canvas.SetTop(currentView, pd.ImageableRect.Top + (pd.ImageableRect.Height - currentView.Height) / 2);
                break;

            case Alignment.MiddleCenter:
                Canvas.SetLeft(currentView, pd.ImageableRect.Left + (pd.ImageableRect.Width - currentView.Width) / 2);
                Canvas.SetTop(currentView, pd.ImageableRect.Top + (pd.ImageableRect.Height - currentView.Height) / 2);
                break;

            case Alignment.MiddleRight:
                Canvas.SetLeft(currentView, pd.ImageableRect.Right - currentView.Width);
                Canvas.SetTop(currentView, pd.ImageableRect.Top + (pd.ImageableRect.Height - currentView.Height) / 2);
                break;

            case Alignment.BottomLeft:
                Canvas.SetLeft(currentView, pd.ImageableRect.Left);
                Canvas.SetTop(currentView, pd.ImageableRect.Bottom - currentView.Height);
                break;

            case Alignment.BottomCenter:
                Canvas.SetLeft(currentView, pd.ImageableRect.Left + (pd.ImageableRect.Width - currentView.Width) / 2);
                Canvas.SetTop(currentView, pd.ImageableRect.Bottom - currentView.Height);
                break;

            case Alignment.BottomRight:
                Canvas.SetLeft(currentView, pd.ImageableRect.Right - currentView.Width);
                Canvas.SetTop(currentView, pd.ImageableRect.Bottom - currentView.Height);
                break;
        }
    }

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        printDocument.SetPreviewPage(e.PageNumber, printCanvas);
    }

    private void AddPages(object sender, AddPagesEventArgs e)
    {
        printDocument.AddPage(printCanvas);
        printDocument.AddPagesComplete();
    }
}
