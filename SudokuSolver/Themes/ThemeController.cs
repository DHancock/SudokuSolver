using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using Sudoku.Common;

#nullable enable

namespace Sudoku.Themes
{             
    internal static class ThemeController
    {
        private sealed class Implementation
        {
            private enum ThemeType { none, light, dark };

            private const string cUID_Key = "842B87BE-910E-467F-9C29-3647D0A16453";
            private const string cLightThemeKeyValue = "light";
            private const string cDarkThemeKeyValue = "dark";
            private const string cLightThemeLocation = "pack://application:,,,/SudokuSolver;component/themes/lighttheme.xaml";
            private const string cDarkThemeLocation = "pack://application:,,,/SudokuSolver;component/themes/darktheme.xaml";

            private readonly ResourceDictionary lightTheme;
            private readonly ResourceDictionary darkTheme;

            public Implementation()
            {
                lightTheme = new ResourceDictionary { Source = new Uri(cLightThemeLocation) };
                darkTheme = new ResourceDictionary { Source = new Uri(cDarkThemeLocation) };
            }

            private static ThemeType IdentifyTheme(string? keyValue)
            {
                if (keyValue is null)
                    return ThemeType.none;
                    
                if (keyValue == cLightThemeKeyValue)
                    return ThemeType.light;

                if (keyValue == cDarkThemeKeyValue)
                    return ThemeType.dark;

                throw new ArgumentOutOfRangeException(nameof(keyValue));
            }

            private static ThemeType FindCurrentTheme(FrameworkElement frameworkElement)
            {
                return IdentifyTheme(frameworkElement.TryFindResource(cUID_Key) as string);
            }

            private static ThemeType FindCurrentTheme(Application app)
            {
                return IdentifyTheme(app.TryFindResource(cUID_Key) as string);
            }

            // This will only remove an existing theme from the current level of the logical
            // tree which is where the new themed resource will be added. It won't remove
            // a theme resource from the children of the item containing these resources.
            private static void RemoveExistingTheme(ResourceDictionary resources)
            {
                Debug.Assert(resources.MergedDictionaries.Count(rd => rd.Contains(cUID_Key)) <= 1);

                ResourceDictionary? theme = resources.MergedDictionaries.FirstOrDefault(rd => rd.Contains(cUID_Key)) ;
                
                if (theme is not null)
                    resources.MergedDictionaries.Remove(theme);
            }

            private void SetTheme(ResourceDictionary resources, ThemeType newTheme, ThemeType currentTheme)
            {
                if (currentTheme != newTheme)
                {
                    resources.BeginInit();

                    if (currentTheme != ThemeType.none)
                        RemoveExistingTheme(resources);

                    if (newTheme != ThemeType.none)
                        resources.MergedDictionaries.Add(newTheme == ThemeType.light ? lightTheme : darkTheme);

                    resources.EndInit();
                }
            }

            public void SetLightTheme(Application app)
            {
                SetTheme(app.Resources, ThemeType.light, FindCurrentTheme(app));
            }

            public void SetDarkTheme(Application app)
            {
                SetTheme(app.Resources, ThemeType.dark, FindCurrentTheme(app));
            }

            public void SetLightTheme(FrameworkElement element)
            {
                SetTheme(element.Resources, ThemeType.light, FindCurrentTheme(element));
            }

            public void SetDarkTheme(FrameworkElement element)
            {
                SetTheme(element.Resources, ThemeType.dark, FindCurrentTheme(element));
            }
        }
        
        private static readonly Lazy<Implementation> lazy = new Lazy<Implementation>(() => { return new Implementation(); }, isThreadSafe: false);

        public static void SetLightTheme(Application app) => lazy.Value.SetLightTheme(app);
        public static void SetDarkTheme(Application app) => lazy.Value.SetDarkTheme(app);
        public static void SetLightTheme(FrameworkElement element) => lazy.Value.SetLightTheme(element);
        public static void SetDarkTheme(FrameworkElement element) => lazy.Value.SetDarkTheme(element);
    }
}
