using Sudoku.ViewModels;
using Sudoku.Utils;


namespace Sudoku.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : SubClassWindow
{
    // according to https://fileinfo.com this extension isn't in use (at least by a popular program)
    private const string cDefaultFilterName = "Sudoku files";
    private const string cDefaultFileExt = ".sdku";

    private const string cDefaultWindowTitle = "Sudoku Solver";

    private readonly IntPtr hWnd;
    private readonly AppWindow appWindow;
    private readonly PrintHelper printHelper;

    public MainWindow()
    {
        InitializeComponent();
        

        Title = cDefaultWindowTitle;

        PuzzleView.ViewModel = new PuzzleViewModel(ReadSettings());

        hWnd = WindowNative.GetWindowHandle(this);
        appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));

        appWindow.Closing += (s, a) =>
        {
            PuzzleView.ViewModel.WindowPlacement = GetWindowPlacement();
            SaveSettings();
        };

        this.Activated += (s, a) =>
        {
            ThemeHelper.Instance.UpdateTheme(PuzzleView.ViewModel.IsDarkThemed);
        };

        if (AppWindowTitleBar.IsCustomizationSupported() && appWindow.TitleBar is not null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            SetTitleBar(CustomTitleBar);
            ThemeHelper.Instance.Register(LayoutRoot, appWindow.TitleBar);
        }
        else
        {
            SetWindowIcon();
            appWindow.Title = CustomTitle.Text;
            CustomTitleBar.Visibility = Visibility.Collapsed;
            ThemeHelper.Instance.Register(LayoutRoot);
        }

        printHelper = new PrintHelper(hWnd, Content.XamlRoot);

        SetWindowPlacement(PuzzleView.ViewModel.WindowPlacement);

        ProcessCommandLine(Environment.GetCommandLineArgs());
    }


    

    private async void ProcessCommandLine(string[] args)
    {
        // args[0] is typically the full path of the executing assembly
        if ((args?.Length == 2) && (Path.GetExtension(args[1]).ToLower() == cDefaultFileExt) && File.Exists(args[1]))
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(args[1]);
            OpenFile(file);
        }
    }


    private void InitializeThemeAndAccent()
    {
        //if (WindowsThemeHelper.GetWindowsBaseColor() == ThemeManager.BaseColorDark)
        //    SetTheme(dark: true);

        //((PuzzleViewModel)DataContext).AccentTitleBar = WindowsThemeHelper.ShowAccentColorOnTitleBarsAndWindowBorders();
        FindColorPaletteResourcesForTheme("Dark");


    }



    private ColorPaletteResources? FindColorPaletteResourcesForTheme(string theme)
    {
        foreach (var themeDictionary in Application.Current.Resources.ThemeDictionaries)
        {
            if (themeDictionary.Key.ToString() == theme)
            {
                if (themeDictionary.Value is SolidColorBrush)
                {
                    return themeDictionary.Value as ColorPaletteResources;
                }
                else if (themeDictionary.Value is ResourceDictionary targetDictionary)
                {
                    foreach (var mergedDictionary in targetDictionary.MergedDictionaries)
                    {
                        if (mergedDictionary is ColorPaletteResources)
                        {
                            return mergedDictionary as ColorPaletteResources;
                        }
                    }
                }
            }
        }
        return null;
    }


    private void ExitClickHandler(object sender, RoutedEventArgs e) => Close();

    private void PrintClickHandler(object sender, RoutedEventArgs e)
    {
        PuzzleView printView = new PuzzleView
        {
            RequestedTheme = ElementTheme.Light,
            ViewModel = PuzzleView.ViewModel,
        };

        printHelper.PrintView(printView);
    }

    public bool IsPrintingSupported => printHelper.IsSupported();

    private async void OpenFile(StorageFile file)
    {
        try
        {
            Stream stream = await file.OpenStreamForReadAsync();
            PuzzleView.ViewModel?.Open(stream);
            Title = $"{cDefaultWindowTitle} - {file.Name}";
        }
        catch (Exception ex)
        {
            Title = cDefaultWindowTitle;
            string heading = $"Failed to open file \"{file.Name}\"";
            //this.ShowModalMessageExternal(heading, ex.Message);
        }
    }


    private void OpenCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = true;  // TODO: is this necessary defaults to true?
    }

    private async void OpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        FileOpenPicker openPicker = new FileOpenPicker();

        InitializeWithWindow.Initialize(openPicker, hWnd);


        openPicker.FileTypeFilter.Add(cDefaultFileExt);


        StorageFile file = await openPicker.PickSingleFileAsync();

        if (file != null)
            OpenFile(file);
       
    }


    private void SaveCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = true;  // TODO: need a dirty flag always true?
    }

    private void SaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {

    }

    private async void SaveAsClickHandler(object sender, RoutedEventArgs e)
    {
        FileSavePicker savePicker = new FileSavePicker();

        InitializeWithWindow.Initialize(savePicker, hWnd);

        savePicker.FileTypeChoices.Add(cDefaultFilterName, new List<string>() { cDefaultFileExt });
        savePicker.SuggestedFileName = "New Puzzle";

        StorageFile file = await savePicker.PickSaveFileAsync();

        if (file != null)
        {
            try
            {
                using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                {
                    PuzzleView.ViewModel?.Save(transaction.Stream.AsStreamForWrite());
                    await transaction.CommitAsync();
                }
            }

            catch (Exception ex)
            {
                string heading = $"Failed to save file \"{file.Name}\"";
                //this.ShowModalMessageExternal(heading, ex.Message);
            }
        }
    }



    private static string ReadSettings()
    {
        string path = GetSettingsFilePath();

        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        return String.Empty;
    }

    private async void SaveSettings()
    {
        try
        {
            string path = GetSettingsFilePath();
            string? directory = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException();

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, PuzzleView.ViewModel?.SerializeSettings(), Encoding.Unicode, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }


    


    private void AboutClickHandler(object sender, RoutedEventArgs e)
    {


    }

}
