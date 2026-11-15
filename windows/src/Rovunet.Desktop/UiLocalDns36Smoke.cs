using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;
using Rovunet.Platform;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunLocalDns36(string report)
    {
        string folder=Path.GetDirectoryName(report)!,phase="start";Directory.CreateDirectory(folder);
        var checks=new Dictionary<string,bool>();int pointers=0,saves=0;var proxyBefore=new WinInetProxy().Read();
        async Task<bool> Wait(string expression){for(int i=0;i<150;i++){if(htmlReady29&&await html29!.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(4))=="true")return true;await Task.Delay(30);}return false;}
        string Native(FrameworkElement control)=>"document.querySelector('[data-native=\""+HtmlId29(control)+"\"]')";
        string Field(FrameworkElement control)=>"document.querySelector('[data-field=\""+HtmlId29(control)+"\"]')";
        string Back()=>"[...document.querySelectorAll('.native-page button')].find(b=>b.getClientRects().length&&b.getAttribute('aria-label')==="+JsonSerializer.Serialize(L.Text("BACK"))+")";
        async Task Click(string expression){
            if(!await Wait(expression+"!=null"))throw new InvalidOperationException("Missing "+phase);
            await html29!.CoreWebView2.ExecuteScriptAsync("("+expression+").scrollIntoView({block:'center',inline:'nearest'})");await Task.Delay(35);
            using var point=JsonDocument.Parse(await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const b="+expression+",r=b.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return{x,y,hit:b.contains(document.elementFromPoint(x,y)),disabled:b.disabled===true}})()"));var p=point.RootElement;
            if(!p.GetProperty("hit").GetBoolean()||p.GetProperty("disabled").GetBoolean())throw new InvalidOperationException("Obscured "+phase);
            foreach(string type in new[]{"mousePressed","mouseReleased"})await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type,x=p.GetProperty("x").GetDouble(),y=p.GetProperty("y").GetDouble(),button="left",clickCount=1}));pointers++;
        }
        try{
            storeSaveFixture28=()=>saves++;L.Language=data.Language="en";data.Theme="dark";Width=480;Height=900;ApplyTheme();Build();await StartHtml29();Activate();html29!.Focus();
            foreach(string language in new[]{"en","fa","ru","zh-CN"})foreach(string theme in new[]{"light","dark","amoled"})foreach(string form in new[]{"editor","manual"}){
                phase=language+"_"+theme+"_"+form;L.Language=data.Language=language;data.Theme=theme;ApplyTheme();Build();
                var original=Importer.Single(ManualProfiles.Dns("dnstt","Local fixture","dns.example.test","192.0.2.9:53","udp",new string('1',64),"","22","fixture","fixture"));
                DialogWindow page;
                if(form=="editor")page=BuildDnsProfileEditor(original);
                else{page=Dialog("ADD");page.Content=DnsManualForm(["DNSTT"],_=>false,new TextBlock());}
                page.Show();QueueHtml29();var input=Visuals<TextBox>(page.PageVisual).Single(t=>Equals(t.Tag,"DNS_RESOLVER_INPUT36"));var local=Visuals<Button>(page.PageVisual).Single(t=>Equals(t.Tag,"DNS_LOCAL_NETWORK"));
                const string manual="[2001:db8::53]:53\n192.0.2.99:53";input.Text=manual;
                checks[phase+"_opening_keeps_manual_ipv6"]=await Wait(Field(input)+"?.value==="+JsonSerializer.Serialize(manual));
                localResolversFixture36=()=>new[]{IPAddress.Parse("2001:db8::53"),IPAddress.Parse("192.0.2.53"),IPAddress.Parse("198.51.100.53"),IPAddress.Parse("192.0.2.53")};
                await Click(Native(local));const string expected="192.0.2.53:53\n198.51.100.53:53";
                checks[phase+"_dual_stack_one_click_only_ipv4_order_and_dedupe"]=await Wait(Field(input)+"?.value==="+JsonSerializer.Serialize(expected))&&input.Text==expected;
                input.Text=manual;localResolversFixture36=()=>new[]{IPAddress.Parse("2001:db8::53")};QueueHtml29();
                await Click(Native(local));
                checks[phase+"_ipv6_only_preserves_previous_and_explains"]=await Wait("document.querySelector('.native-page')?.textContent.includes("+JsonSerializer.Serialize(L.Text("DNS_LOCAL_UNAVAILABLE"))+")")&&input.Text==manual;
                if(form=="editor"&&language is "en" or "fa")using(var image=File.Create(Path.Combine(folder,"local-dns-empty-"+language+"_"+theme+".png")))await html29.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,image);
                await Click(Back());checks[phase+"_return_keeps_manual_ipv6"]=await Wait(Field(input)+"?.value==="+JsonSerializer.Serialize(manual));
                localResolversFixture36=()=>new[]{IPAddress.Parse("198.51.100.54"),IPAddress.Parse("192.0.2.54"),IPAddress.Parse("198.51.100.54")};
                await Click(Native(local));const string v4="198.51.100.54:53\n192.0.2.54:53";
                checks[phase+"_ipv4_only_keeps_existing_order"]=await Wait(Field(input)+"?.value==="+JsonSerializer.Serialize(v4))&&input.Text==v4;
                checks[phase+"_localized_direction_and_touch_target"]=await Wait("(()=>{const b="+Native(local)+",r=b.getBoundingClientRect();return r.height>=44&&r.width>=44&&b.textContent.includes("+JsonSerializer.Serialize(L.Text("DNS_LOCAL_NETWORK"))+")&&document.documentElement.dir==="+JsonSerializer.Serialize(language=="fa"?"rtl":"ltr")+"})()");
                page.Close();
            }
            checks["no_owner_store_write_or_live_connection"]=saves==0&&data.Profiles.Count==0&&client==null&&testing==null;
            checks["host_system_proxy_unchanged"]=proxyBefore==new WinInetProxy().Read();
        }catch(Exception e){checks["exception_at_"+phase]=false;File.WriteAllText(report+".error",e.ToString());}
        finally{foreach(var page in DialogWindow.OpenPages.Where(p=>p.PageHost==this).Reverse().ToArray())page.Close();localResolversFixture36=null;storeSaveFixture28=null;htmlPageTimer29.Stop();durationTimer.Stop();routingTimer28.Stop();}
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,actualPointerClicks=pointers,ownerStoreTouched=false,systemIpv6Changed=false,liveRemoteNetwork=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
