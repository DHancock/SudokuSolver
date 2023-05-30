namespace SudokuSolver.Views;

internal sealed partial class SimpleColorPicker : UserControl
{
    public event TypedEventHandler<SimpleColorPicker, Color>? ColorChanged;

    public SimpleColorPicker()
    {
        this.InitializeComponent();
    }

    public bool MiniPalette
    {
        set => PickButton.Flyout = (Flyout)Resources[value ? "MiniPalette" : "FullPalette"];
    }

    public Color Color
    {
        get { return (Color)GetValue(ColorProperty); }
        set { SetValue(ColorProperty, value); }
    }

    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register(nameof(Color),
            typeof(string),
            typeof(SimpleColorPicker),
            new PropertyMetadata(Colors.Transparent, ColorPropertyChanged));

    private static void ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SimpleColorPicker)d).IndicatorColor.Background = new SolidColorBrush((Color)e.NewValue);
    }

    private void BPE(object sender, PointerRoutedEventArgs e)
    {
        const float cZoomFactor = 1.5f;

        Border border = (Border)sender;
        border.CenterPoint = new Vector3((float)(border.ActualWidth / 2.0), (float)(border.ActualHeight / 2.0), 1f);

        Canvas.SetZIndex(border, 1);
        border.Scale = new Vector3(cZoomFactor, cZoomFactor, 1);
    }

    private void BPX(object sender, PointerRoutedEventArgs e)
    {
        Border border = (Border)sender;

        Canvas.SetZIndex(border, 0);
        border.Scale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void BPR(object sender, PointerRoutedEventArgs e)
    {        
        PickButton.Flyout.Hide();

        Border border = (Border)sender;
        Color newColor = ((SolidColorBrush)border.Background).Color;
        bool colorChanged;

        if (IndicatorColor.Background is null) // color property hasn't been set
            colorChanged = true;
        else
        {
            Color oldColor = ((SolidColorBrush)IndicatorColor.Background).Color;
            colorChanged = newColor != oldColor;
        }

        if (colorChanged)
        {
            IndicatorColor.Background = border.Background;

            Color = newColor;
            ColorChanged?.Invoke(this, newColor);
        }
    }
}
