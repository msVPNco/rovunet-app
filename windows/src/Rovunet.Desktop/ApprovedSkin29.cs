using System.Windows;
using System.Windows.Media;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    Brush PowerMaterial29(bool connected)
    {
        if(SystemParameters.HighContrast)return Tone(connected?"PowerSurface":"AddSurface");
        string key=connected?"power:on29":"power:off29";
        if(glassSurfaces.TryGetValue(key,out var cached))return cached;
        var brush=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(connected?"#C8F8DE":"#E4D9FA"),(Color)ColorConverter.ConvertFromString(connected?"#86C8A7":"#A68BCD"),new Point(0,0),new Point(1,1));
        brush.Freeze();glassSurfaces[key]=brush;return brush;
    }
}
