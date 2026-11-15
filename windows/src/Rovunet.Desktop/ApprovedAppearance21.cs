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
        var palette = !darkTheme ? new Dictionary<string, string>
        {
            ["Bg"]="#F1F1FA", ["BgTop"]="#E9EDF8", ["Card"]="#E5E9F3", ["Soft"]="#FFFFFF",
            ["Sidebar"]="#F1F1FA", ["Ink"]="#1A2A3D", ["Muted"]="#4F6175", ["Accent"]="#6345AC",
            ["Outline"]="#ADB7CB", ["Selected"]="#DED8EB", ["Green"]="#126445", ["Red"]="#AE243F", ["Amber"]="#885706",
            ["PowerSurface"]="#99D7B6", ["PowerInk"]="#173E34", ["AddSurface"]="#D2C4ED", ["AddInk"]="#302445",
            ["Collection"]="#DED8EB", ["StatusSurface"]="#E5E9F3", ["Hero"]="#FBFCFF", ["HeroEnd"]="#F1F1FA", ["ScanCyan"]="#146A78"
        } : IsAmoled ? new Dictionary<string, string>
        {
            ["Bg"]="#030506", ["BgTop"]="#0C1016", ["Card"]="#10151D", ["Soft"]="#171D25",
            ["Sidebar"]="#030506", ["Ink"]="#F1F4FB", ["Muted"]="#C1CCDA", ["Accent"]="#D2C4ED",
            ["Outline"]="#565766", ["Selected"]="#393447", ["Green"]="#A5E5C8", ["Red"]="#FFB5BE", ["Amber"]="#F3CF7D",
            ["PowerSurface"]="#99D7B6", ["PowerInk"]="#173E34", ["AddSurface"]="#D2C4ED", ["AddInk"]="#302445",
            ["Collection"]="#393447", ["StatusSurface"]="#10151D", ["Hero"]="#06080C", ["HeroEnd"]="#030506", ["ScanCyan"]="#83D4DD"
        } : new Dictionary<string, string>
        {
            ["Bg"]="#172437", ["BgTop"]="#263850", ["Card"]="#22354B", ["Soft"]="#2A4057",
            ["Sidebar"]="#172437", ["Ink"]="#F1F4FB", ["Muted"]="#C1CCDA", ["Accent"]="#D2C4ED",
            ["Outline"]="#64758C", ["Selected"]="#655A7E", ["Green"]="#A5E5C8", ["Red"]="#FFB5BE", ["Amber"]="#F3CF7D",
            ["PowerSurface"]="#99D7B6", ["PowerInk"]="#173E34", ["AddSurface"]="#D2C4ED", ["AddInk"]="#302445",
            ["Collection"]="#655A7E", ["StatusSurface"]="#22354B", ["Hero"]="#101B2A", ["HeroEnd"]="#172437", ["ScanCyan"]="#83D4DD"
        };
        foreach (var (key, color) in palette)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze(); Application.Current.Resources[key] = brush;
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
