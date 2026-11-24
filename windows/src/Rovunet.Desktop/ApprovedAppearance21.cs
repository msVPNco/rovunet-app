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
            ["Bg"]="#F5F5F7", ["BgTop"]="#FAFAFB", ["Card"]="#EDF1F6", ["Soft"]="#E8EDF3",
            ["Sidebar"]="#F5F5F7", ["Ink"]="#282B35", ["Muted"]="#505966", ["Accent"]="#74678B",
            ["Outline"]="#C1CAD6", ["Selected"]="#E8E6EF", ["Green"]="#08613B", ["Red"]="#B3261E", ["Amber"]="#8A5A00",
            ["PowerSurface"]="#99D7B6", ["PowerInk"]="#173E34", ["AddSurface"]="#E8E6EF", ["AddInk"]="#2D2C36",
            ["Collection"]="#E8E6EF", ["StatusSurface"]="#EDF1F6", ["Hero"]="#E5EBF2", ["HeroEnd"]="#F5F5F7", ["ScanCyan"]="#146A78"
        } : IsAmoled ? new Dictionary<string, string>
        {
            ["Bg"]="#000000", ["BgTop"]="#101319", ["Card"]="#171D27", ["Soft"]="#202938",
            ["Sidebar"]="#000000", ["Ink"]="#F1F2F5", ["Muted"]="#C6CBD4", ["Accent"]="#C6BDDB",
            ["Outline"]="#465165", ["Selected"]="#222229", ["Green"]="#9CF0C4", ["Red"]="#FFB8BE", ["Amber"]="#E8C15A",
            ["PowerSurface"]="#99D7B6", ["PowerInk"]="#173E34", ["AddSurface"]="#222229", ["AddInk"]="#EEEEF3",
            ["Collection"]="#222229", ["StatusSurface"]="#171D27", ["Hero"]="#283445", ["HeroEnd"]="#000000", ["ScanCyan"]="#83D4DD"
        } : new Dictionary<string, string>
        {
            ["Bg"]="#15171D", ["BgTop"]="#1E2129", ["Card"]="#252F3E", ["Soft"]="#2C3747",
            ["Sidebar"]="#15171D", ["Ink"]="#F1F2F5", ["Muted"]="#C6CBD4", ["Accent"]="#C6BDDB",
            ["Outline"]="#526074", ["Selected"]="#30313B", ["Green"]="#9CF0C4", ["Red"]="#FFB8BE", ["Amber"]="#E8C15A",
            ["PowerSurface"]="#99D7B6", ["PowerInk"]="#173E34", ["AddSurface"]="#30313B", ["AddInk"]="#EEEEF3",
            ["Collection"]="#30313B", ["StatusSurface"]="#252F3E", ["Hero"]="#344357", ["HeroEnd"]="#15171D", ["ScanCyan"]="#83D4DD"
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
