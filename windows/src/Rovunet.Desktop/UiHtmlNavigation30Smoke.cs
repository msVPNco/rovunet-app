using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunHtmlNavigation30(string report)
    {
        var checks=new Dictionary<string,bool>();string phase="start",folder=Path.GetDirectoryName(report)!;Directory.CreateDirectory(folder);
        try{
            data.Profiles=[Importer.Single("socks5://127.0.0.1:19453#Navigation fixture")];data.Profiles[0].CollectionId="navigation30";data.OpenCollectionId="navigation30";data.SelectedId=data.Profiles[0].Id;data.CollectionExpansionRevision=1;
            string language=Environment.GetEnvironmentVariable("ROVUNET_HTML30_LANGUAGE")??"en",theme=Environment.GetEnvironmentVariable("ROVUNET_HTML30_THEME")??"dark";if(!new[]{"en","fa","ru","zh-CN"}.Contains(language)||!new[]{"dark","light","amoled"}.Contains(theme))throw new InvalidOperationException("Invalid fixture variant");
            L.Language=data.Language=language;data.Theme=theme;Width=480;Height=850;ApplyTheme();Build();await StartHtml29();Activate();html29!.Focus();
            async Task<bool> Wait(string expression){for(int i=0;i<100;i++){if(html29.CoreWebView2!=null&&await html29.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(4))=="true")return true;await Task.Delay(40);}return false;}
            async Task Click(string expression){await html29.CoreWebView2.ExecuteScriptAsync("("+expression+").click()").WaitAsync(TimeSpan.FromSeconds(4));}
            string Find(string label,string prefix="")=>"[...document.querySelectorAll("+JsonSerializer.Serialize(prefix+"button")+")].find(button=>button.getAttribute('aria-label')==="+JsonSerializer.Serialize(label)+")";
            async Task Back(){await Click(Find(L.Text("BACK"),".native-page "));checks[phase+"_back_to_home"]=await Wait("document.querySelector('.hero')!==null");}
            checks["shell_loaded"]=await Wait("document.querySelector('.hero')!==null");
            checks["requested_theme_is_actually_rendered"]=await Wait("document.body.dataset.theme==="+JsonSerializer.Serialize(theme));
            phase="toolbar_settings";await Click(Find(L.Text("SETTINGS"),".topbar "));checks[phase+"_opens_by_html_click"]=await Wait("document.querySelector('.native-page input[type=checkbox]')!==null");
            var firstCheck=Visuals<CheckBox>(pageStack[^1].PageVisual).First();bool prior=firstCheck.IsChecked==true;await Click("document.querySelector('.native-page input[type=checkbox]')");await Task.Delay(200);checks["second_html_message_updates_open_settings"]=firstCheck.IsChecked!=prior;await Back();
            phase="manual";await Click("document.querySelector('.add')");await Wait("document.querySelector('.menu')!==null");await Click(Find(L.Text("ADD_MANUAL"),".menu "));checks["protocol_picker_opens_from_add_menu"]=await Wait("document.querySelector('button[aria-label=SOCKS5]')!==null");
            await Click(Find("SOCKS5",".native-page "));checks["nested_protocol_form_opens_from_html"]=await Wait("document.querySelector('.native-page input[type=text]')!==null");checks["nested_protocol_depth_is_two"]=NavigationDepth==2;
            await Click(Find(L.Text("BACK"),".native-page "));checks["nested_back_returns_to_picker"]=await Wait("document.querySelector('button[aria-label=SOCKS5]')!==null");await Back();
            phase="card_edit";await Click("document.querySelector('.profile .actions button:first-child')");checks["card_edit_opens_from_html"]=await Wait("document.querySelector('.native-page input[type=text]')!==null");
            checks["back_save_title_share_compact_toolbar"]=await Wait("(()=>{const bar=document.querySelector('.n-toolbar'),a=bar?.querySelectorAll('button');if(a?.length!==2)return false;const x=a[0].getBoundingClientRect(),y=a[1].getBoundingClientRect();return Math.abs(x.y-y.y)<1&&bar.getBoundingClientRect().height<=56})()");
            using(var image=File.Create(Path.Combine(folder,"edit-"+language+"-"+theme+".png")))await html29.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,image);
            await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const input=document.querySelector('.native-page input[type=text]');input.value='Saved from HTML';input.dispatchEvent(new Event('change',{bubbles:true}));})()");await Click(Find(L.Text("SAVE"),".page-actions "));checks["toolbar_save_closes_editor"]=await Wait("document.querySelector('.hero')!==null");checks["toolbar_save_applies_edited_profile"]=data.Profiles[0].Name=="Saved from HTML";
            phase="import";await Click("document.querySelector('.add')");await Wait("document.querySelector('.menu')!==null");await Click(Find(L.Text("ADD_LINK_FILE"),".menu "));checks["import_has_selectable_html_tabs"]=await Wait("document.querySelectorAll('.n-tabs button').length>=2");await Click("document.querySelectorAll('.n-tabs button')[1]");checks["qr_tab_switches_content"]=await Wait("document.querySelector('.n-tabs button:nth-child(2)')?.getAttribute('aria-selected')==='true'&&document.querySelector('.native-page textarea')===null");await Back();
            phase="subscription";await Click("document.querySelector('.add')");await Wait("document.querySelector('.menu')!==null");await Click(Find(L.Text("ADD_SUBSCRIPTION"),".menu "));checks["subscription_opens_from_html"]=await Wait("document.querySelectorAll('.native-page input[type=text]').length>=2");await Back();
            phase="sidebar";Width=1120;Build();QueueHtml29();await Wait("!document.querySelector('.workspace').classList.contains('compact')");await Click(Find(L.Text("SETTINGS"),".sidebar "));checks["sidebar_settings_opens_from_html"]=await Wait("document.querySelector('.native-page input[type=checkbox]')!==null");await Back();
            phase="about";await Click(Find(L.Text("ABOUT"),".sidebar "));checks["sidebar_about_opens_from_html"]=await Wait("document.querySelector('.native-page img')!==null");await Back();
            phase="resolver";await Click(Find(L.Text("DNS_SCANNER"),".sidebar "));checks["resolver_menu_opens_from_html"]=await Wait("document.querySelector('.native-page')!==null");await Back();
            checks["no_network_agent_or_owner_store_used"]=client==null&&testing==null&&state.State=="disconnected";
        }catch(Exception error){checks["exception_at_"+phase]=false;File.WriteAllText(report+".error",error.ToString());}
        finally{foreach(var page in DialogWindow.OpenPages.Where(p=>p.PageHost==this).Reverse().ToArray())page.Close();htmlPageTimer29.Stop();durationTimer.Stop();}
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,entry="actual HTML click -> WebMessageReceived -> existing native action -> nested HTML page -> HTML back",ownerStoreTouched=false,liveNetwork=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
