using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Rovunet.Core;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunHtml29(string report)
    {
        var checks=new Dictionary<string,bool>();string folder=Path.GetDirectoryName(report)!;Directory.CreateDirectory(folder);var start=DateTimeOffset.UtcNow;
        try{
            htmlFixtureFolder29=folder;HtmlFixture29=true;
            data.Profiles=Enumerable.Range(0,4).Select(i=>{var p=Importer.Single("socks5://127.0.0.1:"+(19400+i)+"#"+Uri.EscapeDataString(i==0?"REALITY 1 · Germany · Frankfurt · نام طولانی برای بررسی متن لغزان و مرز دقیق آن":"Profile "+i));p.CollectionId="html29";p.CollectionName="canada";return p;}).ToList();
            data.SelectedId=data.Profiles[0].Id;data.OpenCollectionId="html29";data.CollectionExpansionRevision=1;data.MarqueeMotion="on";Width=480;Height=900;Build();await StartHtml29();
            async Task<bool> Wait(string expression,int timeout=15000){var until=DateTime.UtcNow.AddMilliseconds(timeout);while(DateTime.UtcNow<until){if(html29?.CoreWebView2!=null&&htmlReady29&&await html29.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")")=="true")return true;await Task.Delay(40);}return false;}
            checks["actual_webview2_loaded_local_shell"]=await Wait("document.querySelectorAll('.profile').length===4",30000);
            if(!checks.Values.Last())throw new InvalidOperationException("HTML shell did not produce its initial model");
            foreach(string language in new[]{"fa","en","ru","zh-CN"})foreach(string theme in new[]{"dark","light","amoled"})foreach(int width in new[]{480,1120}){
                L.Language=data.Language=language;data.Theme=theme;Width=width;ApplyTheme();Build();QueueHtml29();string key=language+theme+width;
                checks[key+"_real_dom_theme_language"]=await Wait("document.body.dataset.theme==="+JsonSerializer.Serialize(theme)+"&&document.documentElement.lang==="+JsonSerializer.Serialize(language)+"&&document.querySelectorAll('.profile').length===4");
                checks[key+"_flags_centered"]=await Wait("(()=>{const r=document.querySelector('.flags').getBoundingClientRect();return Math.abs(r.x+r.width/2-document.documentElement.clientWidth/2)<1})()");
                checks[key+"_no_horizontal_overflow"]=await Wait("document.documentElement.scrollWidth<=document.documentElement.clientWidth");
                checks[key+"_compact_profile_geometry"]=await Wait("Math.abs(document.querySelector('.profile').getBoundingClientRect().height-66)<1");
                checks[key+"_controls_retain_order"]=await Wait("document.querySelector('.collection-top .actions button:nth-child(2)').dataset.test==='quick-ping'&&document.querySelector('.profile .actions button:last-child').getAttribute('aria-label')==="+JsonSerializer.Serialize(L.Text("DELETE")));
                checks[key+"_sidebar_matches_size"]=await Wait(width==480?"document.querySelector('.workspace').classList.contains('compact')":"!document.querySelector('.workspace').classList.contains('compact')");
                File.WriteAllText(Path.Combine(folder,"progress.json"),JsonSerializer.Serialize(new{key,checks,active=IsActive}));
                if(language=="fa"){
                    using var image=File.Create(Path.Combine(folder,theme+"-"+width+".png"));await html29!.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,image);
                }
            }
            L.Language=data.Language="fa";data.Theme="dark";Width=480;ApplyTheme();Build();Activate();html29!.Focus();QueueHtml29();await Wait("document.documentElement.lang==='fa'&&document.querySelector('.profile')!==null");
            string selected=data.SelectedId,order=string.Join(',',data.Profiles.Select(p=>p.Id));
            await html29.CoreWebView2.ExecuteScriptAsync("document.querySelectorAll('.profile')[1].click()");await Task.Delay(120);
            checks["html_first_click_selects_without_reordering_or_connecting"]=data.SelectedId!=selected&&state.State=="disconnected"&&string.Join(',',data.Profiles.Select(p=>p.Id))==order;
            await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.profile .actions button:nth-child(2)').click()");
            checks["html_menu_has_ping_and_accurate_https"]=await Wait("document.querySelector('.menu')?.textContent.includes("+JsonSerializer.Serialize(L.Text("QUICK_PING"))+")&&document.querySelector('.menu')?.textContent.includes("+JsonSerializer.Serialize(L.Text("DETAILED_HTTPS"))+")");
            if(!checks["html_menu_has_ping_and_accurate_https"])File.WriteAllText(Path.Combine(folder,"menu-debug.json"),await html29.CoreWebView2.ExecuteScriptAsync("document.body.innerText"));
            await html29.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.postMessage({command:'close-menu'})");await Wait("!document.querySelector('.menu')");
            SettingsDialogFixture29();await Wait("document.querySelector('.native-page')!==null");
            checks["settings_projected_as_html_controls"]=await Wait("document.querySelectorAll('.native-page button').length>2&&document.querySelectorAll('.native-page input').length>0");
            await CloseNavigation();QueueHtml29();await Wait("document.querySelector('.hero')!==null");
            var form=Dialog("SETTINGS");var fields=new StackPanel();var input=new TextBox{Text="initial"};var check=new CheckBox{Content="Fixture option"};var choice=new ComboBox{ItemsSource=new[]{"One","Two"},SelectedIndex=0};int clicks=0;var action=Button("SAVE",()=>clicks++);fields.Children.Add(input);fields.Children.Add(check);fields.Children.Add(choice);fields.Children.Add(action);form.Content=fields;form.Show();QueueHtml29();await Wait("document.querySelector('.native-page input[type=text]')?.value==='initial'");
            await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const input=document.querySelector('.native-page input[type=text]');input.focus();input.value='Updated from HTML';input.dispatchEvent(new Event('input',{bubbles:true}));})()");await Task.Delay(250);
            checks["html_typing_updates_native_field"]=input.Text=="Updated from HTML";
            await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.native-page input[type=checkbox]').click()");await Task.Delay(120);checks["html_checkbox_updates_native_state"]=check.IsChecked==true;
            await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const select=document.querySelector('.native-page select');select.selectedIndex=1;select.dispatchEvent(new Event('change',{bubbles:true}));})()");await Task.Delay(120);checks["html_select_updates_native_state"]=choice.SelectedIndex==1;
            await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('[data-native=\""+HtmlId29(action)+"\"]').click()");await Task.Delay(120);checks["html_button_calls_existing_handler_once"]=clicks==1;form.Close();QueueHtml29();await Wait("document.querySelector('.hero')!==null");
            var menuCheck=new MenuItem{Header="Fixture menu option",IsCheckable=true};var checkMenu=new ContextMenu();checkMenu.Items.Add(menuCheck);htmlPopup29=HtmlMenu29(checkMenu);QueueHtml29();await Wait("document.querySelector('.menu')!==null");await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.menu button').click()");await Task.Delay(120);checks["html_checkable_menu_changes_state"]=menuCheck.IsChecked;await Wait("!document.querySelector('.menu')");
            var import=BuildAddConfiguration(0);import.Show();QueueHtml29();checks["import_form_is_html_and_usable"]=await Wait("document.querySelector('.native-page textarea')!==null&&document.querySelectorAll('.native-page button').length>=3");import.Close();
            var subscription=BuildSubscriptionPage28(null,null);subscription.Show();QueueHtml29();checks["subscription_form_is_html_and_usable"]=await Wait("document.querySelectorAll('.native-page input[type=text]').length>=2");subscription.Close();
            var resolver=BuildResolverFinder(null);resolver.Show();QueueHtml29();checks["resolver_setup_has_html_fields_and_buttons"]=await Wait("document.querySelectorAll('.native-page input').length>0&&document.querySelectorAll('.native-page button').length>3");resolver.Close();
            var results=Dialog("SCAN_RESULTS");var entries=Enumerable.Range(0,160).Select(i=>new ScanEntry{Address="192.0.2."+(i+1)+":53",Selectable=true,Result=new(){Transport="tcp",Pass=true,Dns=true,ElapsedMS=12}}).ToArray();var resultList=new ListBox{ItemsSource=entries,SelectionMode=SelectionMode.Multiple};results.Content=resultList;results.Show();QueueHtml29();
            checks["html_resolver_results_bound_visible_page"]=await Wait("document.querySelectorAll('.scan-result').length===150");await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.scan-result input').click()");await Task.Delay(120);checks["html_resolver_checkbox_selects_existing_entry"]=resultList.SelectedItems.Contains(entries[0]);
            await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.scan-list > .n-row button:last-child').click()");checks["html_resolver_paging_preserves_remaining_results"]=await Wait("document.querySelectorAll('.scan-result').length===10");
            await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.scan-list > .n-row button:first-child').click()");checks["html_resolver_selection_survives_paging"]=await Wait("document.querySelector('.scan-result input')?.checked===true");results.Close();
            QueueHtml29();await Wait("document.querySelector('.hero')!==null");
            string snapshot=data.SelectedId;await html29.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.postMessage({command:'execute',id:'0',value:'not-an-allowed-action'})");await Task.Delay(80);checks["unknown_bridge_command_is_inert"]=snapshot==data.SelectedId;
            string local=html29.CoreWebView2.Source;html29.CoreWebView2.Navigate("https://blocked.example.invalid/");await Task.Delay(100);checks["external_navigation_is_blocked"]=html29.CoreWebView2.Source==local;
            // Exercise both preferences inside this owned WebView; never change the OS setting.
            await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setEmulatedMedia","{\"features\":[{\"name\":\"prefers-reduced-motion\",\"value\":\"reduce\"}]}");
            checks["reduced_motion_preference_stops_animation"]=await Wait("getComputedStyle(document.querySelector('.profile .roll span')).animationName==='none'");
            await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setEmulatedMedia","{\"features\":[{\"name\":\"prefers-reduced-motion\",\"value\":\"no-preference\"}]}");
            Activate();html29.Focus();QueueHtml29();checks["html_active_window_can_animate"]=await Wait("document.body.classList.contains('active')&&document.querySelector('.profile .roll').dataset.overflow==='true'&&getComputedStyle(document.querySelector('.profile .roll span')).animationPlayState==='running'&&getComputedStyle(document.querySelector('.profile .roll span')).animationName!=='none'");
            await Task.Delay(2300);
            // Clock readings are from the actual renderer, not wall-clock estimates.
            string before=await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.profile .roll span').getAnimations()[0]?.currentTime??-1");
            htmlLastModel29="";QueueHtml29();await Task.Delay(120);string after=await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.profile .roll span').getAnimations()[0]?.currentTime??-1");
            checks["telemetry_render_keeps_marquee_progress"]=double.TryParse(before,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double first)&&double.TryParse(after,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double second)&&first>=2000&&second>=first&&second-first<700;
            var other=new Window{Owner=this,Width=200,Height=120,ShowInTaskbar=false,Title="Rovunet focus fixture"};other.Show();other.Activate();
            checks["html_inactive_marquee_pauses"]=await Wait("!document.body.classList.contains('active')&&getComputedStyle(document.querySelector('.profile .roll span')).animationPlayState==='paused'&&document.querySelector('.profile .roll span').getAnimations().every(a=>a.playState==='paused'&&!a.pending)");
            string frozen=await html29.CoreWebView2.ExecuteScriptAsync("getComputedStyle(document.querySelector('.profile .roll span')).transform");await Task.Delay(180);string held=await html29.CoreWebView2.ExecuteScriptAsync("getComputedStyle(document.querySelector('.profile .roll span')).transform");checks["inactive_transform_stays_fixed"]=frozen==held;
            if(!checks["inactive_transform_stays_fixed"])File.WriteAllText(Path.Combine(folder,"motion-debug.json"),await html29.CoreWebView2.ExecuteScriptAsync("JSON.stringify({active:document.body.className,focus:document.hasFocus(),rows:[...document.querySelectorAll('.profile .roll span')].map(n=>({text:n.textContent,time:n.getAnimations()[0]?.currentTime,state:n.getAnimations()[0]?.playState,pending:n.getAnimations()[0]?.pending,transform:getComputedStyle(n).transform}))})")+"\n"+frozen+"\n"+held);
            other.Close();Activate();html29.Focus();QueueHtml29();
            foreach(string stateName in new[]{"connecting","connected","disconnecting","disconnected"}){activeProfile=data.Profiles[0];SetState(new(){State=stateName});QueueHtml29();checks[stateName+"_html_title_updates_without_language_change"]=await Wait("document.querySelector('.hero-title')?.textContent==="+JsonSerializer.Serialize(L.Text(stateName.ToUpperInvariant())));checks[stateName+"_power_color_matches_state"]=await Wait("document.querySelector('.power').classList.contains('connected')==="+(stateName=="connected"?"true":"false"));}
            await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const spacer=document.createElement('div');spacer.style.height='1800px';document.querySelector('.home-scroll').append(spacer);document.querySelector('.home-scroll').scrollTop=300;})()");
            checks["scrolled_header_reaches_fixed_compact_height"]=await Wait("Math.abs(document.querySelector('.hero').getBoundingClientRect().height-88)<1");
            checks["scrolled_header_title_and_button_stay_inside"]=await Wait("(()=>{const card=document.querySelector('.hero').getBoundingClientRect();return ['.power-frame','.hero-title'].every(s=>{const r=document.querySelector(s).getBoundingClientRect();return r.top>=card.top&&r.bottom<=card.bottom})})()");
            checks["html_uses_local_packaged_assets"]=html29.CoreWebView2.Source==HtmlOrigin29+"/index.html";
        }catch(Exception error){checks["exception"]=false;File.WriteAllText(report+".error",error.ToString());}
        finally{durationTimer.Stop();htmlPageTimer29.Stop();certificateFetchFixture29=null;}
        bool passed=checks.Count>0&&checks.Values.All(value=>value);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,realWebView2=true,ownerStoreTouched=false,liveRemoteNetwork=false,startUtc=start,endUtc=DateTimeOffset.UtcNow,pid=Environment.ProcessId},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
    void SettingsDialogFixture29()=>Dispatcher.BeginInvoke(()=>SettingsDialog());
}
