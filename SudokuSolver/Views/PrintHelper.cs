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

    private PrintSize printSize = PrintSize.Size_80;   // TODO: save these in settings?
    private Alignment printAlignment = Alignment.MiddleCenter;

    private PrintTask? printTask;
    private Panel? rootVisual;
    private PrintPage? printPage;
    private bool currentlyPrinting;
    

    public PrintHelper(Window window, DispatcherQueue dispatcherQueue)
    {
        hWnd = WindowNative.GetWindowHandle(window);
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

    public async Task PrintViewAsync(Canvas printCanvas, FrameworkElement puzzleView)
    {
        try
        {
            Debug.Assert(PrintManager.IsSupported());
            Debug.Assert(!currentlyPrinting);  

            if (PrintManager.IsSupported() && !currentlyPrinting) 
            {
                // printing isn't reentrant
                currentlyPrinting = true;

                // a container for the puzzle
                printPage = new PrintPage();
                printPage.AddChild(puzzleView);

                // the printed object must be part of the visual tree
                rootVisual = printCanvas;
                rootVisual.Children.Clear();
                rootVisual.Children.Add(printPage);

                await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }   

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
    {
        printTask = e.Request.CreatePrintTask("Sudoku Puzzle", PrintTaskSourceRequestedHandler);
        
        printTask.Completed += (s, args) =>
        {
            // this is called after the data is handed off to the spooler(?), not actually printed
            Debug.WriteLine($"print task completed, status: {args.Completion}");

            bool success = dispatcherQueue.TryEnqueue(() =>
            {
                rootVisual?.Children.Clear();
                printPage = null;
                currentlyPrinting = false;
            });
            Debug.Assert(success);
        };
    }

    private void PrintTaskSourceRequestedHandler(PrintTaskSourceRequestedArgs args)
    {
        Debug.Assert(printTask is not null);

        args.SetSource(printDocumentSource);

        PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
        IList<string> displayedOptions = printDetailedOptions.DisplayedOptions;

        // define the default option set
        displayedOptions.Clear();
        displayedOptions.Add(StandardPrintTaskOptions.ColorMode);
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
            {
                bool success = dispatcherQueue.TryEnqueue(() => printDocument.InvalidatePreview());
                Debug.Assert(success);
            }
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
        try
        {
            Debug.Assert(rootVisual is not null);
            Debug.Assert(printPage is not null);

            bool layoutInvalid = false;

            PrintPageDescription pd = e.PrintTaskOptions.GetPageDescription(jobPageNumber: 0);
            layoutInvalid |= printPage.SetPageSize(pd.PageSize);

            double imageSize = Math.Min(pd.ImageableRect.Height, pd.ImageableRect.Width) * ((double)printSize / 100D);
            layoutInvalid |= printPage.SetImageSize(imageSize);

            Point position = default;

            switch (printAlignment)
            {
                case Alignment.TopLeft:
                    position.X = pd.ImageableRect.Left;
                    position.Y = pd.ImageableRect.Top;
                    break;

                case Alignment.TopCenter:
                    position.X = pd.ImageableRect.Left + (pd.ImageableRect.Width - imageSize) / 2;
                    position.Y = pd.ImageableRect.Top;
                    break;

                case Alignment.TopRight:
                    position.X = pd.ImageableRect.Right - imageSize;
                    position.Y = pd.ImageableRect.Top;
                    break;

                case Alignment.MiddleLeft:
                    position.X = pd.ImageableRect.Left;
                    position.Y = pd.ImageableRect.Top + (pd.ImageableRect.Height - imageSize) / 2;
                    break;

                case Alignment.MiddleCenter:
                    position.X = pd.ImageableRect.Left + (pd.ImageableRect.Width - imageSize) / 2;
                    position.Y = pd.ImageableRect.Top + (pd.ImageableRect.Height - imageSize) / 2;
                    break;

                case Alignment.MiddleRight:
                    position.X = pd.ImageableRect.Right - imageSize;
                    position.Y = pd.ImageableRect.Top + (pd.ImageableRect.Height - imageSize) / 2;
                    break;

                case Alignment.BottomLeft:
                    position.X = pd.ImageableRect.Left;
                    position.Y = pd.ImageableRect.Bottom - imageSize;
                    break;

                case Alignment.BottomCenter:
                    position.X = pd.ImageableRect.Left + (pd.ImageableRect.Width - imageSize) / 2;
                    position.Y = pd.ImageableRect.Bottom - imageSize;
                    break;

                case Alignment.BottomRight:
                    position.X = pd.ImageableRect.Right - imageSize;
                    position.Y = pd.ImageableRect.Bottom - imageSize;
                    break;
            }

            layoutInvalid |= printPage.SetImageLocation(position);

            if (layoutInvalid)
            {
                rootVisual.InvalidateMeasure();
                rootVisual.UpdateLayout();
            }

            printDocument.SetPreviewPageCount(count: 1, PreviewPageCountType.Final);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        Debug.Assert(printPage is not null);

        try
        {
            printDocument.SetPreviewPage(e.PageNumber, printPage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private void AddPages(object sender, AddPagesEventArgs e)
    {
        Debug.Assert(printPage is not null);

        try
        {
            printDocument.AddPage(printPage);
            printDocument.AddPagesComplete();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
}
