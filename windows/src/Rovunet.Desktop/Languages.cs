using System.Windows;

namespace Rovunet.Desktop;

/// <summary>
/// Every UI language the Windows client offers, matching the Android build's list.
/// Order is the order shown in the picker: the four original languages first.
/// </summary>
public static class Languages
{
    public sealed record Entry(string Code, string NativeName, FlowDirection Direction, string Font);

    static readonly Entry[] entries =
    [
        new("fa","فارسی",FlowDirection.RightToLeft,"Vazirmatn"),
        new("en","English",FlowDirection.LeftToRight,"Segoe UI"),
        new("ru","Русский",FlowDirection.LeftToRight,"Segoe UI"),
        new("zh-CN","简体中文",FlowDirection.LeftToRight,"Microsoft YaHei UI"),
        new("ar","العربية",FlowDirection.RightToLeft,"Vazirmatn"),
        new("tr","Türkçe",FlowDirection.LeftToRight,"Segoe UI"),
        new("my","မြန်မာ",FlowDirection.LeftToRight,"Myanmar Text"),
        new("ur","اردو",FlowDirection.RightToLeft,"Vazirmatn"),
        new("hi","हिन्दी",FlowDirection.LeftToRight,"Nirmala UI"),
        new("bn","বাংলা",FlowDirection.LeftToRight,"Nirmala UI"),
        new("es","Español",FlowDirection.LeftToRight,"Segoe UI"),
        new("fr","Français",FlowDirection.LeftToRight,"Segoe UI"),
        new("pt","Português",FlowDirection.LeftToRight,"Segoe UI"),
        new("vi","Tiếng Việt",FlowDirection.LeftToRight,"Segoe UI"),
        new("id","Bahasa Indonesia",FlowDirection.LeftToRight,"Segoe UI"),
        new("th","ไทย",FlowDirection.LeftToRight,"Leelawadee UI")
    ];

    // Only languages with a complete locale file are offered, so nothing in the picker
    // silently falls back to English. Adding a Locales/<code>.json makes it appear here.
    public static IReadOnlyList<Entry> All => entries.Where(e => L.Has(e.Code)).ToArray();
    public static IReadOnlyList<string> Codes => entries.Select(e => e.Code).ToArray();
    public static bool IsSupported(string code) => entries.Any(e => e.Code == code);
    public static Entry Get(string code) => entries.FirstOrDefault(e => e.Code == code) ?? entries[1];
    public static FlowDirection DirectionOf(string code) => Get(code).Direction;

    /// <summary>The font that renders this language's script on a stock Windows install.</summary>
    public static string FontOf(string code)
    {
        string preferred = Get(code).Font;
        if (preferred == "Vazirmatn") return preferred; // bundled with the app
        try { return System.Windows.Media.Fonts.SystemFontFamilies.Any(f => f.Source == preferred) ? preferred : "Segoe UI"; }
        catch { return "Segoe UI"; }
    }
}
