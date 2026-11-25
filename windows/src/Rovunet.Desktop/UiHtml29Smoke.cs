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
            // A real pointer click focuses the card before the click fires.
            await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const n=document.querySelectorAll('.profile')[1];n.focus();n.click();})()");await Task.Delay(120);
            checks["html_first_click_selects_without_reordering_or_connecting"]=data.SelectedId!=selected&&state.State=="disconnected"&&string.Join(',',data.Profiles.Select(p=>p.Id))==order;
            // The rebuilt list must keep keyboard focus on the card the user clicked.
            checks["card_click_keeps_focus_on_clicked_card"]=await Wait("document.activeElement?.dataset?.profile==="+JsonSerializer.Serialize(data.SelectedId)+"&&document.querySelector('.profile.selected')?.dataset.profile==="+JsonSerializer.Serialize(data.SelectedId));
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
            checks["explicit_motion_on_overrides_reduced_preference"]=await Wait("getComputedStyle(document.querySelector('.profile .roll-track')).animationName!=='none'");
            data.MarqueeMotion="off";QueueHtml29();checks["explicit_motion_off_stops_animation"]=await Wait("getComputedStyle(document.querySelector('.profile .roll-track')).animationName==='none'");
            data.MarqueeMotion="on";QueueHtml29();
            await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setEmulatedMedia","{\"features\":[{\"name\":\"prefers-reduced-motion\",\"value\":\"no-preference\"}]}");
            Activate();html29.Focus();QueueHtml29();checks["html_active_window_can_animate"]=await Wait("document.body.classList.contains('active')&&document.querySelector('.profile .roll').dataset.overflow==='true'&&getComputedStyle(document.querySelector('.profile .roll-track')).animationPlayState==='running'&&getComputedStyle(document.querySelector('.profile .roll-track')).animationName!=='none'");
            await Task.Delay(2300);
            // Clock readings are from the actual renderer, not wall-clock estimates.
            string before=await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.profile .roll-track').getAnimations()[0]?.currentTime??-1");
            htmlLastModel29="";QueueHtml29();await Task.Delay(120);string after=await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.profile .roll-track').getAnimations()[0]?.currentTime??-1");
            checks["telemetry_render_keeps_marquee_progress"]=double.TryParse(before,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double first)&&double.TryParse(after,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double second)&&first>=2000&&second>=first&&second-first<700;
            var other=new Window{Owner=this,Width=200,Height=120,ShowInTaskbar=false,Title="Rovunet focus fixture"};other.Show();other.Activate();
            checks["explicit_motion_on_survives_blur"]=await Wait("!document.body.classList.contains('active')&&getComputedStyle(document.querySelector('.profile .roll-track')).animationPlayState==='running'");
            string frozen=await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.profile .roll-track').getAnimations()[0]?.currentTime??-1");await Task.Delay(180);string held=await html29.CoreWebView2.ExecuteScriptAsync("document.querySelector('.profile .roll-track').getAnimations()[0]?.currentTime??-1");
            checks["inactive_marquee_clock_advances"]=double.TryParse(frozen,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double blurStart)&&double.TryParse(held,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double blurEnd)&&blurEnd>blurStart;

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

    // Uses the actual window SizeChanged and WebView bridge, not Build() as a
    // substitute for resizing. Only synthetic data and an isolated WebView folder.
    internal async Task<bool> RunHtmlResize41(string report)
    {
        var checks=new Dictionary<string,bool>();var samples=new List<object>();
        string folder=Path.GetDirectoryName(report)!;Directory.CreateDirectory(folder);
        var heartbeat=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal){Interval=TimeSpan.FromMilliseconds(16)};
        var clock=System.Diagnostics.Stopwatch.StartNew();double lastBeat=0,maxGap=0;
        heartbeat.Tick+=(_,_)=>{double now=clock.Elapsed.TotalMilliseconds;if(lastBeat>0)maxGap=Math.Max(maxGap,now-lastBeat);lastBeat=now;};
        async Task<bool> Wait(string expression,int timeout=15000){var until=DateTime.UtcNow.AddMilliseconds(timeout);while(DateTime.UtcNow<until){if(htmlReady29&&html29?.CoreWebView2!=null&&await html29.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")")=="true")return true;await Task.Delay(25);}return false;}
        string Quote(string value)=>JsonSerializer.Serialize(value);
        async Task Shot(string name){using var image=File.Create(Path.Combine(folder,name+".png"));await html29!.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,image);}
        try{
            storeSaveFixture28=()=>{};HtmlFixture29=true;htmlFixtureFolder29=folder;
            foreach(int count in new[]{200,500}){
                data.Profiles=Enumerable.Range(0,count).Select(i=>{var p=Importer.Single("socks5://127.0.0.1:"+(19000+i)+"#"+Uri.EscapeDataString("Synthetic path "+i+" · نام طولانی مسیر آزمایشی"));p.CollectionId="resize41-"+(i%6);p.CollectionName=i%6==5?"مجموعهٔ آزمایشی با نام بسیار طولانی فارسی برای سنجش حرکت متن":"Synthetic collection "+(i%6);return p;}).ToList();
                data.SelectedId=data.Profiles[0].Id;data.OpenCollectionId="resize41-0";data.CollectionExpansionRevision=1;data.MarqueeMotion="on";
                L.Language=data.Language="en";data.Theme="dark";Width=480;Height=900;ApplyTheme();Build();await StartHtml29();Activate();html29!.Focus();QueueHtml29();
                // Wait for this profile set's own render, not the previous one's
                // DOM, or the first timed resize measures that pending render.
                checks[count+"_loaded"]=await Wait("document.querySelectorAll('.collection').length===6&&document.querySelectorAll('.profile').length==="+((count+5)/6));
                await Task.Delay(200);
                var core=html29.CoreWebView2;
                // Timed real resizes; the 240ms settle is excluded from inputMs,
                // but included in the end-to-end settleMs and heartbeat window.
                heartbeat.Start();
                foreach(int width in new[]{1120,480,1120,480}){
                    var nativeRow=rows[0].Tile;await core.ExecuteScriptAsync("window.resizeNode41=document.querySelector('.home-scroll');window.resizeFocus41=document.querySelector('.profile');window.resizeFocus41.focus();window.resizeNode41.scrollTop=30");
                    maxGap=0;lastBeat=clock.Elapsed.TotalMilliseconds;var watch=System.Diagnostics.Stopwatch.StartNew();Width=width;double inputMs=watch.Elapsed.TotalMilliseconds;
                    await Task.Delay(240);bool compact=width<ContextThreshold;
                    bool settled=await Wait("document.querySelector('.workspace').classList.contains('compact')==="+(compact?"true":"false"));
                    double settleMs=watch.Elapsed.TotalMilliseconds;
                    bool nativeSame=ReferenceEquals(nativeRow,rows[0].Tile);
                    bool domSame=await core.ExecuteScriptAsync("window.resizeNode41===document.querySelector('.home-scroll')")=="true";
                    bool focusSame=await core.ExecuteScriptAsync("window.resizeFocus41===document.activeElement")=="true";
                    bool scrollSame=await core.ExecuteScriptAsync("Math.abs(document.querySelector('.home-scroll').scrollTop-30)<1")=="true";
                    samples.Add(new{count,width,inputMs,settleMs,maxDispatcherGapMs=maxGap,nativeCardsPreserved=nativeSame,domPreserved=domSame,focusPreserved=focusSame,scrollPreserved=scrollSame});
                    checks[count+"_"+samples.Count+"_resize_preserves_native_cards"]=nativeSame;
                    checks[count+"_"+samples.Count+"_resize_preserves_dom_focus"]=domSame&&focusSame&&scrollSame;
                    checks[count+"_"+samples.Count+"_resize_settles"]=settled;
                    checks[count+"_"+samples.Count+"_resize_no_second_stall"]=inputMs<500&&maxGap<750;
                }
                foreach(var next in new[]{WindowState.Maximized,WindowState.Normal}){
                    var nativeRow=rows[0].Tile;maxGap=0;lastBeat=clock.Elapsed.TotalMilliseconds;var watch=System.Diagnostics.Stopwatch.StartNew();WindowState=next;double inputMs=watch.Elapsed.TotalMilliseconds;await Task.Delay(300);
                    samples.Add(new{count,windowState=next.ToString(),inputMs,settleMs=watch.Elapsed.TotalMilliseconds,maxDispatcherGapMs=maxGap,nativeCardsPreserved=ReferenceEquals(nativeRow,rows[0].Tile)});
                    checks[count+"_"+next+"_preserves_native_cards"]=ReferenceEquals(nativeRow,rows[0].Tile);
                    checks[count+"_"+next+"_no_second_stall"]=inputMs<500&&maxGap<750;
                }
                heartbeat.Stop();
                // Select via the real HTML click handler, then close its group.
                string selectedBefore=data.SelectedId;
                await core.ExecuteScriptAsync("document.querySelectorAll('.profile')[1].click()");
                checks[count+"_click_selects_disconnected"]=await Wait("document.querySelectorAll('.profile')[1]?.classList.contains('selected')&&document.querySelectorAll('.profile.selected').length===1")&&state.State=="disconnected"&&data.SelectedId!=selectedBefore;
                // An overflowing Latin line must leave the box to the left and keep
                // going; it used to be pushed right and bounce back.
                checks[count+"_latin_overflow_scrolls_outward"]=await Wait("(()=>{const n=[...document.querySelectorAll('.profile-name')].find(n=>n.dataset.overflow==='true');if(!n)return false;const t=parseFloat(getComputedStyle(n).getPropertyValue('--travel'));const a=n.firstElementChild.getAnimations()[0];return t<0&&Math.abs(t)>2&&a&&a.playState==='running'})()");
                checks[count+"_persian_overflow_scrolls_outward"]=await Wait("(()=>{const n=[...document.querySelectorAll('.collection-name')].find(n=>n.dataset.overflow==='true');if(!n)return false;if(getComputedStyle(n).direction!=='rtl')return false;const t=parseFloat(getComputedStyle(n).getPropertyValue('--travel'));return t>2})()");
                string selectedGroup=CollectionKey(Selected!);string groupSelector="[data-group="+Quote(selectedGroup)+"] .collection-head";
                await core.ExecuteScriptAsync("document.querySelector("+Quote(groupSelector+" .actions button:first-child")+").click()");
                checks[count+"_closed_selected_group_visible"]=await Wait("document.querySelectorAll('.profile').length===0&&document.querySelector("+Quote(groupSelector)+")?.classList.contains('selected')");
                // Opening a different group must not move the selection indicator.
                string other=groups.First(g=>g.Key!=selectedGroup).Key;
                await core.ExecuteScriptAsync("document.querySelector("+Quote("[data-group="+Quote(other)+"] .collection-head .actions button:first-child")+").click()");
                checks[count+"_other_group_keeps_selection"]=await Wait("document.querySelector("+Quote(groupSelector)+")?.classList.contains('selected')&&document.querySelectorAll('.profile').length>0");
                foreach(string language in new[]{"fa","en"})foreach(string theme in new[]{"dark","light","amoled"}){
                    L.Language=data.Language=language;data.Theme=theme;ApplyTheme();Build();QueueHtml29();
                    checks[count+"_selected_"+language+"_"+theme]=await Wait("document.documentElement.lang==="+Quote(language)+"&&document.body.dataset.theme==="+Quote(theme)+"&&(()=>{const n=document.querySelector("+Quote(groupSelector)+");if(!n?.classList.contains('selected'))return false;const s=getComputedStyle(n);return s.boxShadow.includes('inset')&&s.borderTopColor!==getComputedStyle(document.querySelector('.collection-head:not(.selected)')).borderTopColor})()");
                    if(count==500&&language=="fa")await Shot("selected-collapsed-"+theme);
                }
                var focusWindow=new Window{Owner=this,Width=200,Height=120,ShowInTaskbar=false,Title="Synthetic focus fixture"};focusWindow.Show();focusWindow.Activate();
                checks[count+"_selection_survives_blur"]=await Wait("document.querySelector("+Quote(groupSelector)+")?.classList.contains('selected')");focusWindow.Close();Activate();
                // Tapping a card itself is a choice: the ring and the open list move
                // to that card, which is what the owner expects when disconnected.
                var tap=System.Diagnostics.Stopwatch.StartNew();
                await core.ExecuteScriptAsync("document.querySelector("+Quote("[data-group="+Quote(other)+"] .collection-label")+").click()");
                checks[count+"_card_tap_selects_that_collection"]=await Wait("document.querySelector("+Quote("[data-group="+Quote(other)+"] .collection-head")+")?.classList.contains('selected')&&document.querySelector("+Quote("[data-group="+Quote(other)+"] .profiles")+")!==null");
                checks[count+"_card_tap_releases_previous"]=await Wait("!document.querySelector("+Quote(groupSelector)+")?.classList.contains('selected')");
                tap.Stop();
                // A card with hundreds of paths must open without a multi-second stall.
                samples.Add(new{count,cardTapMs=tap.ElapsedMilliseconds});
                checks[count+"_card_tap_under_1200ms"]=tap.ElapsedMilliseconds<1200;
            }
            // Choosing an accent must keep Settings open instead of dropping the
            // user back on Home.
            _ = Dispatcher.BeginInvoke(()=>SettingsDialog());
            checks["settings_page_open_for_accent"]=await Wait("document.querySelector('.native-page')!==null");
            string accentBefore=data.AccentColor;
            await html29!.CoreWebView2.ExecuteScriptAsync("(()=>{const b=document.querySelectorAll('.native-page .n-button.swatch')[1];if(b)b.click()})()");
            // The click travels through the WebView bridge, so wait for the state
            // itself instead of assuming the DOM check implies the command ran.
            async Task<bool> State(Func<bool> ready){var until=DateTime.UtcNow.AddSeconds(15);while(DateTime.UtcNow<until){if(ready())return true;await Task.Delay(25);}return false;}
            checks["accent_click_keeps_settings_open"]=await State(()=>data.AccentColor!=accentBefore)
                &&await Wait("document.querySelector('.native-page')!==null")&&NavigationDepth>0;
            string themeBefore=data.Theme;
            await html29!.CoreWebView2.ExecuteScriptAsync("(()=>{const b=[...document.querySelectorAll('.native-page .n-button')].find(n=>n.getAttribute('aria-label')==="+Quote(L.Text("LIGHT_THEME"))+");if(b)b.click()})()");
            checks["theme_click_keeps_settings_open"]=await State(()=>data.Theme=="light")
                &&await Wait("document.querySelector('.native-page')!==null")&&NavigationDepth>0;
            if(themeBefore!="light"){data.Theme=themeBefore;ApplyTheme();QueueHtml29();}
            await CloseNavigation();QueueHtml29();await Wait("document.querySelector('.hero')!==null");
            _ = Dispatcher.BeginInvoke(()=>PublicSourcesPage());
            checks["public_sources_open"]=await Wait("document.querySelector('.native-page')!==null");
            // A key-carrying build must offer the real Free button; a public build
            // must explain itself and offer no action at all.
            checks["free_card_matches_build"]=await Wait(RovunetFreeCatalog.Enabled
                ?"[...document.querySelectorAll('.native-page .n-surface')].some(n=>n.textContent.includes("+Quote(L.Text("SOURCE_ROVUNET"))+")&&n.querySelectorAll('button').length===1&&!n.querySelector('input,a'))"
                :"[...document.querySelectorAll('.native-page .n-surface')].some(n=>n.textContent.includes("+Quote(L.Text("SOURCE_ROVUNET"))+")&&n.textContent.includes("+Quote(L.Text("ROVUNET_FREE_UNAVAILABLE"))+")&&!n.querySelector('button,input,a'))");
            await Shot("free-unavailable");await CloseNavigation();QueueHtml29();await Wait("document.querySelector('.hero')!==null");
            // Native fallback must catch up to a width change skipped by HTML.
            Width=1120;await Task.Delay(240);HtmlFailed29();
            checks["native_fallback_current_layout"]=!htmlReady29&&navigationShell?.Opacity==1&&navigationShell.IsHitTestVisible&&compactLayout==(ActualWidth<ContextThreshold);
        }catch(Exception error){checks["exception"]=false;File.WriteAllText(report+".error",error.ToString());}
        finally{heartbeat.Stop();durationTimer.Stop();htmlPageTimer29.Stop();htmlResize29.Stop();storeSaveFixture28=null;}
        bool passed=checks.Count>0&&checks.Values.All(value=>value);
        File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,samples,realWebView2=true,ownerStoreTouched=false,liveRemoteNetwork=false,storageLatencyMeasured=false,version=Version},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
