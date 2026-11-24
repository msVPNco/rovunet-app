using System.Windows;
using System.Windows.Media;
namespace Rovunet.Desktop;
internal static class Typography
{
    internal static double Size(double original)=>original;
    internal static FontWeight Body=>FontWeights.Normal;
    internal static FontFamily Family(string language)
    {
        string code=Languages.Get(language).Code;
        // Vazirmatn is bundled and covers Persian, Arabic and Urdu shapes.
        if(Languages.Get(code).Font=="Vazirmatn")return new FontFamily(new Uri("pack://application:,,,/Rovunet;component/"),"./Assets/#Vazirmatn");
        string preferred=Languages.FontOf(code);
        return new FontFamily(preferred);
    }
}
