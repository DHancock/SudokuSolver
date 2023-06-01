using SudokuSolver.Utilities;

namespace SudokuSolver.Views;

internal sealed partial class SimpleColorPicker : UserControl
{
    private const int cSamplesPerColor = 10;  // the number of color shades
    private DateTime lastKeyRepeat = DateTime.UtcNow;

    public event TypedEventHandler<SimpleColorPicker, Color>? ColorChanged;

    public SimpleColorPicker()
    {
        this.InitializeComponent();
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

    public bool MiniPalette
    {
        set
        {
            VariableSizedWrapGrid grid = (VariableSizedWrapGrid)((Flyout)PickButton.Flyout).Content;

            if (grid.Children.Count == 0)
            {
                int index = 0;

                if (value)
                {
                    grid.MaximumRowsOrColumns = sMiniPaletteColumnOffsets.Length; // columns

                    for (int colorSample = 0; colorSample < cSamplesPerColor; colorSample++)
                    {
                        for (int numColors = 0; numColors < grid.MaximumRowsOrColumns; numColors++)
                        {
                            Border border = CreateBorder();
                            border.Background = new SolidColorBrush(ConvertToColor(sRGB[sMiniPaletteColumnOffsets[numColors] + colorSample]));
                            border.Tag = index++;

                            grid.Children.Add(border);
                        }
                    }
                }
                else
                {
                    grid.MaximumRowsOrColumns = sRGB.Length / cSamplesPerColor; // columns

                    for (int colorSample = 0; colorSample < cSamplesPerColor; colorSample++)
                    {
                        for (int numColors = 0; numColors < grid.MaximumRowsOrColumns; numColors++)
                        {
                            Border border = CreateBorder();
                            border.Background = new SolidColorBrush(ConvertToColor(sRGB[(cSamplesPerColor * numColors) + colorSample]));
                            border.Tag = index++;

                            grid.Children.Add(border);
                        }
                    }
                }
            }
        }
    }

    private Border CreateBorder()
    {
        Border border = new Border();

        border.ScaleTransition = new Vector3Transition();
        border.PointerEntered += Border_PointerEntered;
        border.PointerExited += Border_PointerExited;
        border.PointerReleased += Border_PointReleased;
        border.GotFocus += Border_GotFocus;
        border.LostFocus += Border_LostFocus;
        border.KeyUp += Border_KeyUp;
        border.KeyDown += Border_KeyDown;

        return border;
    }

    private static Color ConvertToColor(uint rgb)
    {
        Color color = default;
        color.A = 0xFF;
        color.R = (byte)(rgb >> 16);
        color.G = (byte)((rgb >> 8) & 0x000000FF);
        color.B = (byte)(rgb & 0x000000FF);
        return color;
    }

    private static void ZoomColorOut(Border border)
    {
        const float cZoomFactor = 1.5f;

        border.CenterPoint = new Vector3((float)(border.ActualWidth / 2.0), (float)(border.ActualHeight / 2.0), 1f);
        Canvas.SetZIndex(border, 1);
        border.Scale = new Vector3(cZoomFactor, cZoomFactor, 1);
    }

    private static void ZoomColorIn(Border border)
    {
        Canvas.SetZIndex(border, 0);
        border.Scale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private static void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Border border = (Border)sender;

        if (border.IsTabStop)
            border.Focus(FocusState.Programmatic);
        else
            ZoomColorOut(border);
    }

    private static void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Border border = (Border)sender;

        if (!border.IsTabStop)
            ZoomColorIn(border);
    }

    private void Border_PointReleased(object sender, PointerRoutedEventArgs e) 
    {
        CloseFlyout((Border)sender);
    }

    private static void Border_GotFocus(object sender, RoutedEventArgs e) 
    {
        ZoomColorOut((Border)sender);
    }

    private static void Border_LostFocus(object sender, RoutedEventArgs e)
    {
        ZoomColorIn((Border)sender);
    }

    private void CloseFlyout(Border border)
    {
        Color newColor = ((SolidColorBrush)border.Background).Color;

        if (newColor != Color)
        {
            Color = newColor;
            ColorChanged?.Invoke(this, newColor);
        }

        SetTabStopStateWithinFlyout(enable: false);
        PickButton.Flyout.Hide();
    }


    private void Border_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)  // key ups are only received if tab stop is true
            CloseFlyout((Border)sender);
    }

    private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if ((e.Key == VirtualKey.Up) || (e.Key == VirtualKey.Down) || (e.Key == VirtualKey.Left) || (e.Key == VirtualKey.Right))
        {
            if ((DateTime.UtcNow - lastKeyRepeat) > TimeSpan.FromMilliseconds(130)) // throttle focus changes
            {
                VariableSizedWrapGrid grid = (VariableSizedWrapGrid)((Flyout)PickButton.Flyout).Content;

                int colorsInRow = grid.Children.Count / cSamplesPerColor; // the number of columns           
                int index = (int)((Border)sender).Tag;
                int total = grid.Children.Count;
                int newIndex;

                if (e.Key == VirtualKey.Up)
                {
                    newIndex = Utils.Clamp2DVerticalIndex(index - colorsInRow, colorsInRow, total);
                }
                else if (e.Key == VirtualKey.Down)
                {
                    newIndex = Utils.Clamp2DVerticalIndex(index + colorsInRow, colorsInRow, total);
                }
                else if (e.Key == VirtualKey.Left)
                {
                    newIndex = Utils.Clamp2DHorizontalIndex(index - 1, total);
                }
                else
                {
                    newIndex = Utils.Clamp2DHorizontalIndex(index + 1, total);
                }

                lastKeyRepeat = DateTime.UtcNow;

                grid.Children[newIndex].Focus(FocusState.Programmatic);
            }
        }
    }

    private void PickButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (!sender.Flyout.IsOpen) // it's being opened via the keyboard
        {
            SetTabStopStateWithinFlyout(enable: true);
            sender.Flyout.ShowAt(sender);
        }
    }

    private void SetTabStopStateWithinFlyout(bool enable)
    {
        VariableSizedWrapGrid grid = (VariableSizedWrapGrid)((Flyout)PickButton.Flyout).Content;

        foreach (UIElement child in grid.Children)
            child.IsTabStop = enable;
    }


    private readonly static int[] sMiniPaletteColumnOffsets = { 0, 20, 40, 60, 80, 100, 120, 140, 150, 180 };
    
    private readonly static uint[] sRGB =
    {
        // Red
        0x5F1616,
        0x7F1D1D,
        0x991B1B,
        0xB91C1C,
        0xEF1010,
        0xEF3434,
        0xF87171,
        0xFCA5A5,
        0xFECACA,
        0xFEE2E2,
        // Orange
        0x58200D,
        0x7C2D12,
        0x9A3412,
        0xC2410C,
        0xEA580C,
        0xF97316,
        0xFB923C,
        0xFDBA74,
        0xFED7AA,
        0xFFEDD5,
        // Amber
        0x5A270B,
        0x78350F,
        0x92400E,
        0xB45309,
        0xD97706,
        0xF59E0B,
        0xFBBF24,
        0xFCD34D,
        0xFDE68A,
        0xFEF3C7,
        // Yellow
        0x532E0D,
        0x713F12,
        0x854D0E,
        0xA16207,
        0xCA8A04,
        0xEAB308,
        0xFACC15,
        0xFDE047,
        0xFEF08A,
        0xFEF9C3,
        // Lime
        0x23350C,
        0x365314,
        0x3F6212,
        0x4D7C0F,
        0x65A30D,
        0x84CC16,
        0xA3E635,
        0xBEF264,
        0xD9F99D,
        0xECFCCB,
        // Green
        0x0E3D20,
        0x14532D,
        0x166534,
        0x15803D,
        0x16A34A,
        0x22C55E,
        0x4ADE80,
        0x86EFAC,
        0xBBF7D0,
        0xDCFCE7,
        // Emerald
        0x043D2E,
        0x064E3B,
        0x065F46,
        0x047857,
        0x059669,
        0x10B981,
        0x34D399,
        0x6EE7B7,
        0xA7F3D0,
        0xD1FAE5,
        // Teal
        0x0F3D39,
        0x134E4A,
        0x115E59,
        0x0F766E,
        0x0D9488,
        0x14B8A6,
        0x2DD4BF,
        0x5EEAD4,
        0x99F6E4,
        0xCCFBF1,
        // Cyan
        0x124153,
        0x164E63,
        0x155E75,
        0x0E7490,
        0x0891B2,
        0x06B6D4,
        0x22D3EE,
        0x67E8F9,
        0xA5F3FC,
        0xCFFAFE,
        // Light Blue
        0x0A3D5B,
        0x0C4A6E,
        0x075985,
        0x0369A1,
        0x0284C7,
        0x0EA5E9,
        0x38BDF8,
        0x7DD3FC,
        0xBAE6FD,
        0xE0F2FE,
        // Blue
        0x152960,
        0x1E3A8A,
        0x1E40AF,
        0x1D4ED8,
        0x2563EB,
        0x3B82F6,
        0x60A5FA,
        0x93C5FD,
        0xBFDBFE,
        0xDBEAFE,
        // Indigo
        0x222059,
        0x312E81,
        0x3730A3,
        0x4338CA,
        0x4F46E5,
        0x6366F1,
        0x818CF8,
        0xA5B4FC,
        0xC7D2FE,
        0xE0E7FF,
        // Violet
        0x311361,
        0x4C1D95,
        0x5B21B6,
        0x6D28D9,
        0x7C3AED,
        0x8B5CF6,
        0xA78BFA,
        0xC4B5FD,
        0xDDD6FE,
        0xEDE9FE,
        // Purple
        0x361154,
        0x581C87,
        0x6B21A8,
        0x7E22CE,
        0x9333EA,
        0xA855F7,
        0xC084FC,
        0xD8B4FE,
        0xE9D5FF,
        0xF3E8FF,
        // Fuchsia
        0x46104A,
        0x701A75,
        0x86198F,
        0xA21CAF,
        0xC026D3,
        0xD946EF,
        0xE879F9,
        0xF0ABFC,
        0xF5D0FE,
        0xFAE8FF,
        // Pink
        0x5E1131,
        0x831843,
        0x9D174D,
        0xBE185D,
        0xDB2777,
        0xEC4899,
        0xF472B6,
        0xF9A8D4,
        0xFBCFE8,
        0xFCE7F3,
        // Rose
        0x550B22,
        0x881337,
        0x9F1239,
        0xBE123C,
        0xE11D48,
        0xF43F5E,
        0xFB7185,
        0xFDA4AF,
        0xFECDD3,
        0xFFE4E6,
        // Blue Gray
        0x090E1A,
        0x0F172A,
        0x1E293B,
        0x334155,
        0x475569,
        0x64748B,
        0x94A3B8,
        0xCBD5E1,
        0xE2E8F0,
        0xF1F5F9,
        // True Gray
        0x000000,
        0x171717,
        0x262626,
        0x404040,
        0x525252,
        0x737373,
        0xA3A3A3,
        0xD4D4D4,
        0xE5E5E5,
        0xFFFFFF
    };
}
