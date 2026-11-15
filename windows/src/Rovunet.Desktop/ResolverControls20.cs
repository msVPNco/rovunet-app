using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rovunet.Core;

namespace Rovunet.Desktop;

public sealed partial class MainWindow
{
    internal static string DnsTransportLabel20(string value)=>value switch{"tls"=>"DoT","https"=>"DoH","both"=>"UDP + TCP",_=>value.ToUpperInvariant()};
    sealed class DnsTransportConverter20:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>DnsTransportLabel20(value?.ToString()??"");
        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
    }
    static void LabelDnsTransport20(ComboBox model)
    {
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding{Converter=new DnsTransportConverter20()});model.ItemTemplate=new DataTemplate{VisualTree=text};
    }
    internal static string[] NetworkResolvers20()=>LocalResolverPolicy36.SelectIpv4(NetworkInterface.GetAllNetworkInterfaces()
        .Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType is not(NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
        .OrderByDescending(n=>n.GetIPProperties().GatewayAddresses.Count>0)
        .SelectMany(n=>n.GetIPProperties().DnsAddresses));
    Func<IEnumerable<IPAddress>>? localResolversFixture36;
    string[] LocalNetworkResolvers36()=>fixtureMode&&localResolversFixture36!=null?LocalResolverPolicy36.SelectIpv4(localResolversFixture36()):NetworkResolvers20();
}
