using System;
using System.Windows;

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

            private static ThemeType IdentifyTheme(string? themeType)
            {
                if (themeType == cLightThemeKeyValue)
                    return ThemeType.light;

                if (themeType == cDarkThemeKeyValue)
                    return ThemeType.dark;

                return ThemeType.none;
            }

            private static ThemeType FindCurrentTheme(FrameworkElement frameworkElement)
            {
                return IdentifyTheme(frameworkElement.TryFindResource(cUID_Key) as string);
            }

            private static ThemeType FindCurrentTheme(Application app)
            {
                return IdentifyTheme(app.TryFindResource(cUID_Key) as string);
            }

            private static void DeleteExistingTheme(ResourceDictionary resources)
            {
                ResourceDictionary? existingTheme = null;

                // this will only delete at the current level of the tree
                // because this is where the themed resource will be added
                foreach (ResourceDictionary rd in resources.MergedDictionaries)
                {
                    if (rd.Contains(cUID_Key))
                    {
                        existingTheme = rd;
                        break;
                    }
                }

                if (existingTheme is not null)
                    resources.MergedDictionaries.Remove(existingTheme);
            }

            private void SetTheme(ResourceDictionary resources, ThemeType newTheme, ThemeType currentTheme)
            {
                if (currentTheme != newTheme)
                {
                    resources.BeginInit();

                    if (currentTheme != ThemeType.none)
                        DeleteExistingTheme(resources);

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
        
        private static Lazy<Implementation> imp = new Lazy<Implementation>(() => { return new Implementation(); });

        public static void SetLightTheme(Application app) => imp.Value.SetLightTheme(app);
        public static void SetDarkTheme(Application app) => imp.Value.SetDarkTheme(app);
        public static void SetLightTheme(FrameworkElement element) => imp.Value.SetLightTheme(element);
        public static void SetDarkTheme(FrameworkElement element) => imp.Value.SetDarkTheme(element);
    }
}
