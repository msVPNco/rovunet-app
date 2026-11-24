using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rovunet.Desktop;

public sealed partial class MainWindow
{
    static IEnumerable<FrameworkElement> LayoutElements(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement element) yield return element;
            foreach (var nested in LayoutElements(child)) yield return nested;
        }
    }

    // HTML owns visible layout. Keep its lightweight header/navigation model in
    // sync, but defer the native home layout/card rebuild until native fallback.
    void ResizeHtml41(double width)
    {
        nativeLayoutDeferred41 = true;
        bool compact = width < ContextThreshold;
        if (compact == compactLayout || navigationShell == null) return;
        compactLayout = compact;
        navigationShell.Children.Remove(header21);
        header21.Children.Clear(); // release the unchanged action controls
        header21 = CreateHomeHeader41(headerActions41);
        navigationHeader21 = header21;
        Grid.SetColumnSpan(header21, 2);
        navigationShell.Children.Add(header21);
        if (!compact && sidebarRows20.Count == 0) PopulateContext();
        QueueHtml29();
    }

    void RebuildForResize()
    {
        string query = search.Text;
        bool searchVisible = search.Visibility == Visibility.Visible;
        int caret = search.SelectionStart, length = search.SelectionLength;
        double offset = mainScroll.VerticalOffset;
        var focused = Keyboard.FocusedElement as FrameworkElement;
        bool searchFocused = search.IsKeyboardFocusWithin;
        string name = focused == null ? "" : AutomationProperties.GetName(focused);
        var type = focused?.GetType();
        var matches = LayoutElements((DependencyObject)Content)
            .Where(e => e.GetType() == type && AutomationProperties.GetName(e) == name).ToList();
        int index = focused == null ? -1 : matches.IndexOf(focused);
        Build(); search.Text = query; search.Visibility = searchVisible ? Visibility.Visible : Visibility.Collapsed; UpdateLayout();
        if (searchFocused)
        {
            search.Focus(); search.Select(Math.Min(caret, query.Length), Math.Min(length, query.Length - Math.Min(caret, query.Length)));
        }
        else if (index >= 0 && name.Length > 0)
        {
            var replacement = LayoutElements((DependencyObject)Content)
                .Where(e => e.GetType() == type && AutomationProperties.GetName(e) == name).ElementAtOrDefault(index);
            if (replacement is { IsEnabled: true, IsVisible: true }) replacement.Focus();
        }
        // Focus may call BringIntoView; restore the user's reading position afterwards.
        UpdateLayout(); mainScroll.ScrollToVerticalOffset(offset); UpdateLayout();
    }
}
