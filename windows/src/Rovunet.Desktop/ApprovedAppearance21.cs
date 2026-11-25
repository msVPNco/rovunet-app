using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rovunet.Desktop;

public sealed partial class MainWindow
{
    bool MarqueeEnabled21 => !htmlReady29&&(data.MarqueeMotion switch
    {
        "on" => true,
        "off" => false,
        _ => SystemParameters.ClientAreaAnimation
    });

    void ApplyApprovedPalette21()
    {
        // Roles follow the Android Material scheme: background, surface, containers
        // and outline, so the native fallback and the HTML shell read as one app.
        var palette = !darkTheme ? new Dictionary<string, string>
        {
            ["Bg"]="#F6F7F9", ["BgTop"]="#FFFFFF", ["Card"]="#FFFFFF", ["Soft"]="#F2F5F9",
            ["Sidebar"]="#F6F7F9", ["Ink"]="#16181D", ["Muted"]="#5A6472", ["Accent"]="#5B4EA8",
            ["Outline"]="#E4E7EE", ["Selected"]="#ECEAFB", ["Green"]="#1E7B44", ["Red"]="#B23A3A", ["Amber"]="#8A5A00",
            ["PowerSurface"]="#1E7B44", ["PowerInk"]="#FFFFFF", ["AddSurface"]="#16181D", ["AddInk"]="#F6F7F9",
            ["Collection"]="#F2F5F9", ["StatusSurface"]="#FFFFFF", ["Hero"]="#EFF3F9", ["HeroEnd"]="#F6F7F9", ["ScanCyan"]="#146A78"
        } : IsAmoled ? new Dictionary<string, string>
        {
            ["Bg"]="#000000", ["BgTop"]="#0B0D10", ["Card"]="#101318", ["Soft"]="#14181F",
            ["Sidebar"]="#000000", ["Ink"]="#ECEEF3", ["Muted"]="#9BA3B2", ["Accent"]="#B3A6FF",
            ["Outline"]="#22262F", ["Selected"]="#1A1F27", ["Green"]="#5FE0A6", ["Red"]="#FF9AA2", ["Amber"]="#F2C65A",
            ["PowerSurface"]="#5FE0A6", ["PowerInk"]="#04160E", ["AddSurface"]="#1A1F27", ["AddInk"]="#ECEEF3",
            ["Collection"]="#14181F", ["StatusSurface"]="#101318", ["Hero"]="#171C24", ["HeroEnd"]="#000000", ["ScanCyan"]="#7FD3DE"
        } : new Dictionary<string, string>
        {
            ["Bg"]="#0E1014", ["BgTop"]="#14171D", ["Card"]="#171B22", ["Soft"]="#1B2029",
            ["Sidebar"]="#0E1014", ["Ink"]="#ECEEF3", ["Muted"]="#9BA3B2", ["Accent"]="#B3A6FF",
            ["Outline"]="#2A303B", ["Selected"]="#1E2430", ["Green"]="#5FE0A6", ["Red"]="#FF9AA2", ["Amber"]="#F2C65A",
            ["PowerSurface"]="#5FE0A6", ["PowerInk"]="#04160E", ["AddSurface"]="#1E2430", ["AddInk"]="#ECEEF3",
            ["Collection"]="#1E2430", ["StatusSurface"]="#171B22", ["Hero"]="#1F2531", ["HeroEnd"]="#0E1014", ["ScanCyan"]="#7FD3DE"
        };
        foreach (var (key, color) in palette)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze(); Application.Current.Resources[key] = brush;
        }
        // Accent overrides the per-mode defaults; high contrast still wins below.
        if (!SystemParameters.HighContrast)
        {
            var accent = Accent27(data.AccentColor);
            foreach (var (key, color) in new[]
            {
                ("Accent", darkTheme ? accent.Dark : accent.Light),
                ("Selected", darkTheme ? accent.SelectedDark : accent.SelectedLight),
                ("Collection", darkTheme ? accent.SelectedDark : accent.SelectedLight)
            })
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                brush.Freeze(); Application.Current.Resources[key] = brush;
            }
        }
        if (SystemParameters.HighContrast)
        {
            foreach (var key in new[] { "PowerSurface", "AddSurface" }) Application.Current.Resources[key] = SystemColors.HighlightBrush;
            foreach (var key in new[] { "PowerInk", "AddInk" }) Application.Current.Resources[key] = SystemColors.HighlightTextBrush;
            foreach (var key in new[] { "BgTop", "Hero", "HeroEnd", "Collection", "StatusSurface" }) Application.Current.Resources[key] = SystemColors.WindowBrush;
        }
    }

    MarqueeLine MovingLine21(TextBlock text, double height, FlowDirection? direction = null)
    {
        text.TextWrapping = TextWrapping.NoWrap; text.TextTrimming = TextTrimming.None;
        text.Margin = new(0); text.LineHeight = height; text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        var reading = direction ?? ReadingDirection21(text.Text);
        text.FlowDirection = reading;
        return new MarqueeLine(text, height, reading, () => MarqueeEnabled21);
    }
    static FlowDirection ReadingDirection21(string text)
    {
        foreach (char c in text)
        {
            if (!char.IsLetter(c)) continue;
            return c is >= '\u0590' and <= '\u08FF' ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        return L.Direction;
    }
}

// Physical left and right slots never affect the mathematical center of the flags.
// If the slots would overlap, only the flags move to a second, centered row.
internal sealed class CenteredHeader21 : Panel
{
    internal required FrameworkElement Leading { get; init; }
    internal required FrameworkElement Trailing { get; init; }
    internal required FrameworkElement Flags { get; init; }
    internal bool Split { get; private set; }
    const double Edge = 10;
    internal void Initialize() { FlowDirection = FlowDirection.LeftToRight; Children.Add(Leading); Children.Add(Trailing); Children.Add(Flags); }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (UIElement child in Children) child.Measure(new Size(double.PositiveInfinity, 48));
        double width = double.IsFinite(availableSize.Width) ? availableSize.Width : 480;
        Split = width < 2 * Math.Max(Leading.DesiredSize.Width, Trailing.DesiredSize.Width) + Flags.DesiredSize.Width + 2 * Edge + 4;
        return new Size(width, Split ? 106 : 65);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = Split ? 5 : (finalSize.Height - 44) / 2;
        Leading.Arrange(new Rect(Edge, y, Leading.DesiredSize.Width, 44));
        Trailing.Arrange(new Rect(finalSize.Width - Edge - Trailing.DesiredSize.Width, y, Trailing.DesiredSize.Width, 44));
        Flags.Arrange(new Rect((finalSize.Width - Flags.DesiredSize.Width) / 2, Split ? 55 : (finalSize.Height - 44) / 2, Flags.DesiredSize.Width, 44));
        return finalSize;
    }
}
