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
        foreach (var lang in new[] { "fa", "en", "ru", "zh-CN" })
        { using var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(n => n.EndsWith(".Locales." + lang + ".json", StringComparison.Ordinal)))!; a[lang] = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!; }
        return a;
    }
    public static string Language { get; set; } = "fa";
    public static string Text(string key) => all.GetValueOrDefault(Language, all["en"]).GetValueOrDefault(key, all["en"].GetValueOrDefault(key, all["en"]["UNEXPECTED_ERROR"]));
    public static FlowDirection Direction => Language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
