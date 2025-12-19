// based largely on the following:
// https://github.com/marb2000/PrintSample/blob/master/MainWindow.xaml.cs
// https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/Printing/cs/PrintHelper.cs

using SudokuSolver.ViewModels;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace SudokuSolver.Views;

internal sealed class PrintHelper
{
    private readonly IntPtr hWnd;
    private readonly DispatcherQueue dispatcherQueue;
    private readonly PrintManager printManager;
    private readonly PrintDocument printDocument;
    private readonly IPrintDocumentSource printDocumentSource;

    private Settings.PerPrintSettings? settings;

    private PrintTask? printTask;
    private PrintPage? printPage;
    public bool IsCurrentlyPrinting { get; private set; } = false;   

    public PrintHelper(MainWindow window)
    {
        hWnd = window.WindowHandle;
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

    public async Task PrintPuzzleAsync(XElement sessionData)
    {
        Debug.Assert(PrintManager.IsSupported());
        Debug.Assert(!IsCurrentlyPrinting);

        IsCurrentlyPrinting = true;
        settings = Settings.Instance.PrintSettings.Clone();

        // a user control containing a puzzle view
        printPage = new PrintPage(sessionData);

        await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
    }   

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
    {
        printTask = e.Request.CreatePrintTask("Sudoku Puzzle", PrintTaskSourceRequestedHandler);
        
        printTask.Completed += (s, args) =>
        {
            // called after the data is handed off to the spooler, not printed
            printPage = null;
            IsCurrentlyPrinting = false;
        };
    }

    private void PrintTaskSourceRequestedHandler(PrintTaskSourceRequestedArgs args)
    {
        Debug.Assert(printTask is not null);
        Debug.Assert(settings is not null);

        args.SetSource(printDocumentSource);

        PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
        IList<string> displayedOptions = printDetailedOptions.DisplayedOptions;

        // add custom size option
        PrintCustomItemListOptionDetails sizeOption;

        ResourceLoader rl = App.Instance.ResourceLoader;

        sizeOption = printDetailedOptions.CreateItemListOption(CustomOption.PrintSize.ToString(), rl.GetString("PrintSizeHeader"));

        string sizeTemplate = rl.GetString("PrintSizeTemplate");
        string fitToPage = rl.GetString("PrintFitToPage");

        foreach (PrintSize size in Enum.GetValues<PrintSize>().Reverse())
        {
            sizeOption.AddItem(size.ToString(), size == PrintSize.Size_100 ? fitToPage : string.Format(sizeTemplate, (int)size));
        }

        sizeOption.TrySetValue(settings.PrintSize.ToString());
        displayedOptions.Add(sizeOption.OptionId);

        // add custom alignment option
        PrintCustomItemListOptionDetails alignOption;
        alignOption = printDetailedOptions.CreateItemListOption(CustomOption.Alignment.ToString(), rl.GetString("PrintPositionHeader"));
        alignOption.AddItem(Alignment.TopLeft.ToString(), rl.GetString("PrintTopLeft"));
        alignOption.AddItem(Alignment.TopCenter.ToString(), rl.GetString("PrintTopCenter"));
        alignOption.AddItem(Alignment.TopRight.ToString(), rl.GetString("PrintTopRight"));
        alignOption.AddItem(Alignment.MiddleLeft.ToString(), rl.GetString("PrintMiddleLeft"));
        alignOption.AddItem(Alignment.MiddleCenter.ToString(), rl.GetString("PrintMiddleCenter"));
        alignOption.AddItem(Alignment.MiddleRight.ToString(), rl.GetString("PrintMiddleRight"));
        alignOption.AddItem(Alignment.BottomLeft.ToString(), rl.GetString("PrintBottomLeft"));
        alignOption.AddItem(Alignment.BottomCenter.ToString(), rl.GetString("PrintBottomCenter"));
        alignOption.AddItem(Alignment.BottomRight.ToString(), rl.GetString("PrintBottomRight"));

        alignOption.TrySetValue(settings.PrintAlignment.ToString());
        displayedOptions.Add(alignOption.OptionId);

        // add margin option
        PrintCustomItemListOptionDetails marginOption;
        marginOption = printDetailedOptions.CreateItemListOption(CustomOption.Margin.ToString(), rl.GetString("PrintMarginsHeader"));
        marginOption.AddItem(Margin.None.ToString(), rl.GetString("PrintNone"));
        marginOption.AddItem(Margin.Small.ToString(), rl.GetString("PrintSmall"));
        marginOption.AddItem(Margin.Medium.ToString(), rl.GetString("PrintMedium"));
        marginOption.AddItem(Margin.Large.ToString(), rl.GetString("PrintLarge"));

        marginOption.TrySetValue(settings.PrintMargin.ToString());
        displayedOptions.Add(marginOption.OptionId);

        // add heading (file name) option
        PrintCustomToggleOptionDetails header = printDetailedOptions.CreateToggleOption(CustomOption.ShowHeader.ToString(), rl.GetString("PrintFileNameHeader"));
        header.TrySetValue(settings.ShowHeader);
        displayedOptions.Add(header.OptionId);

        printDetailedOptions.OptionChanged += (sender, args) =>
        {
            bool invalidatePreview = false;
            string optionId = (string)args.OptionId;

            if (CustomOption.PrintSize.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                settings.PrintSize = Enum.Parse<PrintSize>((string)option.Value);
                Settings.Instance.PrintSettings.PrintSize = settings.PrintSize;
                invalidatePreview = true;
            }
            else if (CustomOption.Alignment.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                settings.PrintAlignment = Enum.Parse<Alignment>((string)option.Value);
                Settings.Instance.PrintSettings.PrintAlignment = settings.PrintAlignment;
                invalidatePreview = true;
            }
            else if (CustomOption.Margin.ToString() == optionId)
            {
                PrintCustomItemListOptionDetails option = (PrintCustomItemListOptionDetails)sender.Options[optionId];
                settings.PrintMargin = Enum.Parse<Margin>((string)option.Value);
                Settings.Instance.PrintSettings.PrintMargin = settings.PrintMargin;
                invalidatePreview = true;
            }
            else if (CustomOption.ShowHeader.ToString() == optionId)
            {
                PrintCustomToggleOptionDetails option = (PrintCustomToggleOptionDetails)sender.Options[optionId];
                settings.ShowHeader = (bool)option.Value;
                Settings.Instance.PrintSettings.ShowHeader = settings.ShowHeader;
                invalidatePreview = true;
            }

            if (invalidatePreview)
            {
                bool success = dispatcherQueue.TryEnqueue(() => printDocument.InvalidatePreview());
                Debug.Assert(success);
            }
        };
    }

    public enum PrintSize
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

    public enum Alignment
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

    public enum Margin
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
            Debug.Assert(printPage is not null);
            Debug.Assert(settings is not null);

            PrintPageDescription pd = e.PrintTaskOptions.GetPageDescription(jobPageNumber: 0);

            bool layoutInvalid = printPage.SetPageSize(pd.PageSize);

            // adjust the imageable areas for the selected margins
            double marginPercentage = 0.025 * (int)settings.PrintMargin ;
            double margin = Math.Min(pd.ImageableRect.Height, pd.ImageableRect.Width) * marginPercentage;

            // the custom margins are uniform, the printer's may not be
            double leftMargin = Math.Max(margin, pd.ImageableRect.X);
            double topMargin = Math.Max(margin, pd.ImageableRect.Y);
            double rightMargin = Math.Max(margin, pd.PageSize.Width - (pd.ImageableRect.X + pd.ImageableRect.Width));
            double bottomMargin = Math.Max(margin, pd.PageSize.Height - (pd.ImageableRect.Y + pd.ImageableRect.Height));

            Rect imagableArea = new Rect(leftMargin, topMargin, pd.PageSize.Width - (leftMargin + rightMargin), pd.PageSize.Height - (topMargin + bottomMargin));

            // set header if required
            double headerHeight = 0;

            if (settings.ShowHeader)
            {
                headerHeight = printPage.GetHeaderHeight();
                layoutInvalid |= printPage.SetHeadingLocation(new Point(leftMargin, topMargin));
                layoutInvalid |= printPage.SetHeadingWidth(pd.PageSize.Width - (leftMargin + rightMargin));
            }

            layoutInvalid |= printPage.ShowHeader(settings.ShowHeader);

            // adjust the size of the puzzle depending on the margins and header
            double printSizeRatio = (int)settings.PrintSize / 100.0;
            double imageSize = Math.Min(imagableArea.Height - headerHeight, imagableArea.Width) * printSizeRatio;
            layoutInvalid |= printPage.SetPuzzleSize(imageSize);

            // set the position of the puzzle with in the imageable area taking into account the heading 
            double puzzleAreaTop = imagableArea.Top + headerHeight;
            double puzzleAreaHeight = imagableArea.Height - headerHeight;
            Point position = default;

            switch (settings.PrintAlignment)
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
                printPage.InvalidateMeasure();
                printPage.UpdateLayout();
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
