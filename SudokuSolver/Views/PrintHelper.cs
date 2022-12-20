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
    private Margin printMargin = Margin.None;
    private bool showHeader = false;

    private PrintTask? printTask;
    private Panel? rootVisual;
    private PrintPage? printPage;
    private bool currentlyPrinting;
    private string? headerText;
    

    public PrintHelper(Window window)
    {
        hWnd = WindowNative.GetWindowHandle(window);
        dispatcherQueue = window.DispatcherQueue;

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

    public async Task PrintViewAsync(Canvas printCanvas, PuzzleView puzzleView, StorageFile? file)
    {
        try
        {
            Debug.Assert(PrintManager.IsSupported());
            Debug.Assert(!currentlyPrinting);  

            if (PrintManager.IsSupported() && !currentlyPrinting) 
            {
                // printing isn't reentrant
                currentlyPrinting = true;

                headerText = file is null ? App.cNewPuzzleName : file.Path;

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

        // add custom size option
        PrintCustomItemListOptionDetails sizeOption;
        sizeOption = printDetailedOptions.CreateItemListOption(CustomOption.PrintSize.ToString(), "Scale");

        foreach (PrintSize size in Enum.GetValues<PrintSize>().Reverse())
        {
            sizeOption.AddItem(size.ToString(), size == PrintSize.Size_100 ? "Fit to page" : $"{(int)size}%");
        }

        sizeOption.TrySetValue(printSize.ToString());
        displayedOptions.Add(sizeOption.OptionId);

        // add custom alignment option
        PrintCustomItemListOptionDetails alignOption;
        alignOption = printDetailedOptions.CreateItemListOption(CustomOption.Alignment.ToString(), "Position on page");
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


        // add margin option
        PrintCustomItemListOptionDetails marginOption;
        marginOption = printDetailedOptions.CreateItemListOption(CustomOption.Margin.ToString(), "Print margins");
        marginOption.AddItem(Margin.None.ToString(), "none");
        marginOption.AddItem(Margin.Small.ToString(), "small");
        marginOption.AddItem(Margin.Medium.ToString(), "medium");
        marginOption.AddItem(Margin.Large.ToString(), "large");

        marginOption.TrySetValue(printMargin.ToString());
        displayedOptions.Add(marginOption.OptionId);

        // add heading (file name) option
        PrintCustomToggleOptionDetails header = printDetailedOptions.CreateToggleOption(CustomOption.ShowHeader.ToString(), "Print file name");
        header.TrySetValue(showHeader);
        displayedOptions.Add(header.OptionId);

        printDetailedOptions.OptionChanged += (sender, args) =>
        {
            bool invalidatePreview = false;
            string optionId = (string)args.OptionId;

            if (CustomOption.PrintSize.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                printSize = Enum.Parse<PrintSize>((string)option.Value);
                invalidatePreview = true;
            }
            else if (CustomOption.Alignment.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                printAlignment = Enum.Parse<Alignment>((string)option.Value);
                invalidatePreview = true;
            }
            else if (CustomOption.Margin.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                printMargin = Enum.Parse<Margin>((string)option.Value);
                invalidatePreview = true;
            }
            else if (CustomOption.ShowHeader.ToString() == optionId)
            {
                PrintCustomToggleOptionDetails option = (PrintCustomToggleOptionDetails)sender.Options[optionId];
                showHeader = (bool)option.Value;
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
        Size_10 = 10,
        Size_20 = 20,
        Size_30 = 30,
        Size_40 = 40,
        Size_50 = 50,
        Size_60 = 60,
        Size_70 = 70,
        Size_80 = 80,
        Size_90 = 90,
        Size_100 = 100,
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

    private enum Margin
    {
        None,
        Small,
        Medium,
        Large,
    }

    private enum CustomOption
    {
        PrintSize,
        Alignment,
        Margin,
        ShowHeader,
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

            // adjust the imageable areas for the selected margins
            const double cMarginPercentageStep = 2.5;
            double marginPercentage = 0.0;

            switch (printMargin)
            {
                case Margin.Small: marginPercentage = cMarginPercentageStep; break;
                case Margin.Medium: marginPercentage = cMarginPercentageStep * 2; break;
                case Margin.Large: marginPercentage = cMarginPercentageStep * 3; break;
            }

            double margin = Math.Min(pd.ImageableRect.Height, pd.ImageableRect.Width) * (marginPercentage / 100D);

            double x = Math.Max(margin, pd.ImageableRect.X);
            double y = Math.Max(margin, pd.ImageableRect.Y);

            // the custom margins are uniform, the printer's may not be
            double rightMargin = Math.Max(margin, pd.PageSize.Width - (pd.ImageableRect.X + pd.ImageableRect.Width));
            double bottomMargin = Math.Max(margin, pd.PageSize.Height - (pd.ImageableRect.Y + pd.ImageableRect.Height));

            Rect imagableArea = new Rect(x, y, pd.PageSize.Width - (x + rightMargin), pd.PageSize.Height - (y + bottomMargin));

            // set header if required
            double headerHeight = 0;

            if (showHeader)
            {
                printPage.SetHeaderText(headerText);
                headerHeight = printPage.GetHeaderHeight();
                printPage.SetHeadingLocation(new Point(x, y));
                printPage.SetHeadingWidth(pd.PageSize.Width - (x + rightMargin));
            }

            layoutInvalid |= printPage.ShowHeader(showHeader);

            // adjust the size of the puzzle depending on the imageable area, margins and header
            double imageSize = Math.Min(imagableArea.Height - headerHeight, imagableArea.Width) * ((double)printSize / 100D);
            layoutInvalid |= printPage.SetPuzzleSize(imageSize);

            // set the position of the puzzle with in the imageable area taking into account the heading 
            double puzzleAreaTop = imagableArea.Top + headerHeight;
            double puzzleAreaHeight = imagableArea.Height - headerHeight;
            Point position = default;

            switch (printAlignment)
            {
                case Alignment.TopLeft:
                    position.X = imagableArea.Left;
                    position.Y = puzzleAreaTop;
                    break;

                case Alignment.TopCenter:
                    position.X = imagableArea.Left + (imagableArea.Width - imageSize) / 2;
                    position.Y = puzzleAreaTop;
                    break;

                case Alignment.TopRight:
                    position.X = imagableArea.Right - imageSize;
                    position.Y = puzzleAreaTop;
                    break;

                case Alignment.MiddleLeft:
                    position.X = imagableArea.Left;
                    position.Y = puzzleAreaTop + (puzzleAreaHeight - imageSize) / 2;
                    break;

                case Alignment.MiddleCenter:
                    position.X = imagableArea.Left + (imagableArea.Width - imageSize) / 2;
                    position.Y = puzzleAreaTop + (puzzleAreaHeight - imageSize) / 2;
                    break;

                case Alignment.MiddleRight:
                    position.X = imagableArea.Right - imageSize;
                    position.Y = puzzleAreaTop + (puzzleAreaHeight - imageSize) / 2;
                    break;

                case Alignment.BottomLeft:
                    position.X = imagableArea.Left;
                    position.Y = imagableArea.Bottom - imageSize;
                    break;

                case Alignment.BottomCenter:
                    position.X = imagableArea.Left + (imagableArea.Width - imageSize) / 2;
                    position.Y = imagableArea.Bottom - imageSize;
                    break;

                case Alignment.BottomRight:
                    position.X = imagableArea.Right - imageSize;
                    position.Y = imagableArea.Bottom - imageSize;
                    break;
            }

            layoutInvalid |= printPage.SetPuzzleLocation(position);

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
