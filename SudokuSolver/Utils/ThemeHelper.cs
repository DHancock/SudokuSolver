namespace Sudoku.Utils;

internal sealed class ThemeHelper
{
    public static readonly ThemeHelper Instance = new ThemeHelper();

    // this is a single window app
    private AppWindowTitleBar? titleBar;
    private FrameworkElement? content;

    private ThemeHelper()
    {
    }

    public void Register(FrameworkElement content, AppWindowTitleBar? titleBar)
    {
        this.content = content;
        this.titleBar = titleBar;
    }

    public void Register(FrameworkElement content) => Register(content, null);

    public void UpdateTheme(bool isDarkThemed)
    {
        ElementTheme theme = isDarkThemed ? ElementTheme.Dark : ElementTheme.Light;

        if (titleBar is not null)
            UpdateTitleBar(theme);

        UpdateContent(theme);
    }

    private void UpdateContent(ElementTheme requestedTheme)
    {
        Debug.Assert(content is not null);

        if (content.RequestedTheme != requestedTheme)
            content.RequestedTheme = requestedTheme;
    }

    private void UpdateTitleBar(ElementTheme requestedTheme)
    {
        Debug.Assert(titleBar is not null);
        Debug.Assert(AppWindowTitleBar.IsCustomizationSupported());

        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
        titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        if (requestedTheme == ElementTheme.Light)
        {
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonHoverBackgroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
        }
        else
        {
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = Colors.DimGray;
            titleBar.ButtonInactiveForegroundColor = Colors.DimGray;
        }
    }

    public ElementTheme CurrentTheme
    {
        get
        {
            Debug.Assert(content is not null);

            if (content is not null)
                return content.RequestedTheme;

            return App.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
        }
    }
}
