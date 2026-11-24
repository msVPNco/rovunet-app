using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Rovunet.Desktop;

public static class L
{
    static readonly Dictionary<string, Dictionary<string, string>> all = Load();
    // Only resource-string objects are translated. Text typed by the user,
    // profile names, addresses and licence prose are never matched by value.
    static readonly Dictionary<string,string> literalKeys=LiteralKeys();
    static Dictionary<string,string> LiteralKeys(){var result=new Dictionary<string,string>(ReferenceEqualityComparer.Instance);foreach(var language in all.Values)foreach(var entry in language)if(entry.Value.Length>0)result.TryAdd(entry.Value,entry.Key);return result;}
    internal static string CurrentLiteral(string value)=>literalKeys.TryGetValue(value,out var key)?Text(key):value;
    internal static bool Has(string code)=>all.ContainsKey(code);
    internal static void RefreshTree(DependencyObject root)
    {
        var visited=new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);void Visit(DependencyObject current){
            if(!visited.Add(current))return;
            if(current is TextBlock text)text.Text=CurrentLiteral(text.Text);
            if(current is ContentControl content&&content.Content is string caption)content.Content=CurrentLiteral(caption);
            if(current is HeaderedContentControl header&&header.Header is string title)header.Header=CurrentLiteral(title);
            if(current is FrameworkElement element){if(element.ToolTip is string tip)element.ToolTip=CurrentLiteral(tip);string name=AutomationProperties.GetName(element);if(name.Length>0)AutomationProperties.SetName(element,CurrentLiteral(name));}
            foreach(var child in LogicalTreeHelper.GetChildren(current).OfType<DependencyObject>())Visit(child);
        }Visit(root);
    }
    static Dictionary<string, Dictionary<string, string>> Load()
    {
        var a = new Dictionary<string, Dictionary<string, string>>(); var assembly = Assembly.GetExecutingAssembly();
        // Every locale shipped under Locales/ is loaded, so adding a language file is
        // all it takes to offer that language; the language list itself lives in
        // Languages.cs and stays identical to the Android client's order.
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            const string marker = ".Locales.";
            int index = resource.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0 || !resource.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            string code = resource.Substring(index + marker.Length).Replace(".json", "", StringComparison.OrdinalIgnoreCase);
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream == null) continue;
            a[code] = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        }
        return a;
    }
    public static string Language { get; set; } = "fa";
    // An untranslated key falls back to English, then to the raw key. Showing the key
    // is better than an unrelated placeholder sentence.
    public static string Text(string key)
    {
        if (all.TryGetValue(Language, out var table) && table.TryGetValue(key, out var value) && value.Length > 0) return value;
        if (all.TryGetValue("en", out var english) && english.TryGetValue(key, out value) && value.Length > 0) return value;
        return key;
    }
    public static FlowDirection Direction => Languages.DirectionOf(Language);
}
