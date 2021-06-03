using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;

using Sudoku.ViewModels;
using Sudoku.Themes;

#nullable enable

namespace Sudoku.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // according to https://fileinfo.com this extension isn't in use (at least by a popular program)
        private const string cFileFilter = "Sudoku files|*.sdku";
        private const string cDefaultFileExt = ".sdku";
        private const string cDefaultWindowTitle = "Sudoku Solver";


        public MainWindow()
        {
            InitializeComponent();

            Title = cDefaultWindowTitle;
            Activated += MainWindow_Activated;

            InitializeThemeAndAccent();
            ProcessCommandLine(Environment.GetCommandLineArgs());
        }


        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            // this app's light/dark theme setting over rides the OS setting
            // so only check if the title bar and borders setting has changed
            PuzzleViewModel vm = (PuzzleViewModel)DataContext;
            vm.AccentTitleBar = WindowsThemeHelper.ShowAccentColorOnTitleBarsAndWindowBorders();
        }


        private void ProcessCommandLine(string[] args)
        {
            // args[0] is typically the full path of the executing assembly
            if ((args?.Length == 2) && (Path.GetExtension(args[1]).ToLower() == cDefaultFileExt) && File.Exists(args[1]))
                OpenFile(args[1]);
        }

        private void InitializeThemeAndAccent()
        {
            if (WindowsThemeHelper.GetWindowsBaseColor() == ThemeManager.BaseColorDark)
                SetTheme(dark: true);

            ((PuzzleViewModel)DataContext).AccentTitleBar = WindowsThemeHelper.ShowAccentColorOnTitleBarsAndWindowBorders();
        }

        private void ExitClickHandler(object sender, RoutedEventArgs e) => Close();

        private void PrintExecutedHandler(object sender, ExecutedRoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog
            {
                UserPageRangeEnabled = false,
                CurrentPageEnabled = false
            };

            if (printDialog.ShowDialog() == true)
            {
                const double cPaddingPercentage = 6.25;

                PuzzleView puzzleView = new PuzzleView
                {
                    Padding = new Thickness(Math.Min(printDialog.PrintableAreaHeight, printDialog.PrintableAreaWidth) * (cPaddingPercentage / 100D)),
                    DataContext = this.DataContext
                };

                // always print using the light theme
                ThemeController.SetLightTheme(puzzleView);

                printDialog.PrintVisual(puzzleView, "Sudoku puzzle");
            }
        }

        private void OpenFile(string fullPath)
        {
            try
            {
                using FileStream fs = File.OpenRead(fullPath);
                ((PuzzleViewModel)DataContext).Open(fs);
                Title = $"{cDefaultWindowTitle} - {Path.GetFileNameWithoutExtension(fullPath)}";
            }
            catch (Exception ex)
            {
                Title = cDefaultWindowTitle;
                string heading = $"Failed to open file \"{Path.GetFileNameWithoutExtension(fullPath)}\"";
                this.ShowModalMessageExternal(heading, ex.Message);
            }
        }
                                                                             
        private void OpenExecutedHandler(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = cFileFilter };

            if (dialog.ShowDialog() == true)
                OpenFile(dialog.FileName);
        }

        private void SaveExecutedHandler(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog { Filter = cFileFilter };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using Stream stream = dialog.OpenFile();
                    ((PuzzleViewModel)DataContext).Save(stream);
                }
                catch (Exception ex)
                {
                    string heading = $"Failed to save file \"{Path.GetFileNameWithoutExtension(dialog.FileName)}\"";
                    this.ShowModalMessageExternal(heading, ex.Message);
                }
            }
        }

        private void SetTheme(bool dark)
        {
            ((PuzzleViewModel)DataContext).DarkThemed = dark;

            if (dark)
                ThemeController.SetDarkTheme(Application.Current);
            else
                ThemeController.SetLightTheme(Application.Current);
        }

        private void DarkThemeCheckedHandler(object sender, RoutedEventArgs e) => SetTheme(dark: true);

        private void DarkThemeUncheckedHandler(object sender, RoutedEventArgs e) => SetTheme(dark: false);
    }
}
