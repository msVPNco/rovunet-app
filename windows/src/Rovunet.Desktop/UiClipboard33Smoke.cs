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
    internal async Task<bool> RunClipboard33(string report)
    {
        string folder=Path.GetDirectoryName(report)!,phase="start",clip="",nativeScope="not_attempted";
        Directory.CreateDirectory(folder);var checks=new Dictionary<string,bool>();int reads=0,pointers=0,requests=0,saves=0;var proxyBefore=new WinInetProxy().Read();
        const string url="https://subscription.example.test/feed?fixture=33&format=full";
        DataObject? originalClipboard=null;bool replaceClipboard=false,originalEmpty=false,nativeRoundtrip=false;
        async Task<bool> Wait(string expression){for(int i=0;i<150;i++){if(htmlReady29&&await html29!.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(4))=="true")return true;await Task.Delay(30);}return false;}
        string Native(FrameworkElement control)=>"document.querySelector('[data-native=\""+HtmlId29(control)+"\"]')";
        string Field(FrameworkElement control)=>"document.querySelector('[data-field=\""+HtmlId29(control)+"\"]')";
        string Find(string label)=>"[...document.querySelectorAll('.native-page button')].find(b=>b.getClientRects().length&&b.getAttribute('aria-label')==="+JsonSerializer.Serialize(label)+")";
        async Task Click(string expression){
            if(!await Wait(expression+"!=null"))throw new InvalidOperationException("Missing "+phase);
            await html29!.CoreWebView2.ExecuteScriptAsync("("+expression+").scrollIntoView({block:'center',inline:'nearest'})");await Task.Delay(35);
            using var point=JsonDocument.Parse(await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const b="+expression+",r=b.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return{x,y,hit:b.contains(document.elementFromPoint(x,y)),disabled:b.disabled===true}})()"));var p=point.RootElement;
            if(!p.GetProperty("hit").GetBoolean()||p.GetProperty("disabled").GetBoolean())throw new InvalidOperationException("Obscured "+phase);
            foreach(string type in new[]{"mousePressed","mouseReleased"})await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type,x=p.GetProperty("x").GetDouble(),y=p.GetProperty("y").GetDouble(),button="left",clickCount=1}));pointers++;
        }
        (DialogWindow Page,TextBox Input,Button Paste,TextBlock Feedback) Page(string? initial=null){
            var page=BuildSubscriptionPage28(initialUrl:initial);page.Show();QueueHtml29();
            return(page,Visuals<TextBox>(page.PageVisual).Single(t=>Equals(t.Tag,"SUBSCRIPTION_URL_INPUT")),Visuals<Button>(page.PageVisual).Single(t=>Equals(t.Tag,"SUBSCRIPTION_PASTE")),Visuals<TextBlock>(page.PageVisual).Single(t=>Equals(t.Tag,"SUBSCRIPTION_CLIPBOARD_FEEDBACK")));
        }
        try{
            storeSaveFixture28=()=>saves++;subscriptionClipboardReadFixture33=()=>{reads++;return clip;};
            SubscriptionClient.HttpFixture=_=>new UpdateFixtureHandler28((_,_)=>{requests++;throw new InvalidOperationException("Clipboard actions must not fetch");});
            L.Language=data.Language="en";data.Theme="dark";Width=480;Height=900;ApplyTheme();Build();await StartHtml29();Activate();html29!.Focus();
            foreach(string language in new[]{"en","fa","ru","zh-CN"})foreach(string theme in new[]{"light","dark","amoled"}){
                phase=language+"_"+theme;L.Language=data.Language=language;data.Theme=theme;ApplyTheme();Build();var current=Page();int readBefore=reads;
                checks[phase+"_one_localized_paste_and_no_input_copy"]=await Wait(Find(L.Text("PASTE"))+"!=null")&&Visuals<Button>(current.Page.PageVisual).Count(b=>Equals(b.Tag,"SUBSCRIPTION_PASTE"))==1&&!Visuals<Button>(current.Page.PageVisual).Any(b=>Equals(b.Tag,"SUBSCRIPTION_COPY"));
                clip="  "+url+"  ";await Click(Native(current.Paste));
                checks[phase+"_one_click_pastes_url_into_native_and_html"]=await Wait(Field(current.Input)+"?.value==="+JsonSerializer.Serialize(url))&&current.Input.Text==url&&reads==readBefore+1&&current.Feedback.Text==L.Text("PASTED");
                checks[phase+"_paste_touch_target_and_language_direction"]=await Wait("(()=>{const r="+Native(current.Paste)+".getBoundingClientRect();return r.height>=44&&r.width>=44&&document.documentElement.dir==="+JsonSerializer.Serialize(language=="fa"?"rtl":"ltr")+"})()");
                string latest=url+"&last="+language;await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const i="+Field(current.Input)+";i.focus();i.value="+JsonSerializer.Serialize(latest)+";i.dispatchEvent(new Event('input',{bubbles:true}))})()");
                clip=" ";await Click(Native(current.Paste));checks[phase+"_empty_clipboard_preserves_existing_url"]=await Wait("document.querySelector('.native-page')?.textContent.includes("+JsonSerializer.Serialize(L.Text("CLIPBOARD_EMPTY"))+")")&&current.Input.Text==latest&&current.Feedback.Text==L.Text("CLIPBOARD_EMPTY")&&current.Feedback.Foreground==Red;
                subscriptionClipboardReadFixture33=()=>throw new System.Runtime.InteropServices.ExternalException();await Click(Native(current.Paste));
                checks[phase+"_busy_clipboard_is_visible_without_erasing_input"]=await Wait("document.querySelector('.native-page')?.textContent.includes("+JsonSerializer.Serialize(L.Text("CLIPBOARD_UNAVAILABLE"))+")")&&current.Input.Text==latest&&current.Feedback.Text==L.Text("CLIPBOARD_UNAVAILABLE")&&current.Feedback.Foreground==Red;
                subscriptionClipboardReadFixture33=()=>{reads++;return clip;};
                if(language is "en" or "fa")using(var image=File.Create(Path.Combine(folder,"subscription-clipboard-"+phase+".png")))await html29.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,image);
                await Click(Find(L.Text("BACK")));
                foreach(var preset in PublicFeed.Presets){
                    phase=language+"_"+theme+"_"+preset.Key;current=Page(preset.Url);int hiddenReads=reads;
                    checks[phase+"_managed_source_has_no_raw_url_clipboard_actions"]=await Wait(Native(current.Paste)+"===null")&&current.Input.Text=="";
                    current.Paste.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                    checks[phase+"_stale_hidden_action_cannot_access_clipboard"]=reads==hiddenReads;current.Page.Close();
                }
            }
            phase="paste_managed_alias";L.Language=data.Language="en";data.Theme="dark";ApplyTheme();Build();var managed=Page();clip=PublicFeed.MahsaUrl;await Click(Native(managed.Paste));
            checks["paste_managed_url_switches_to_hidden_source_and_keeps_consent_required"]=await Wait(Native(managed.Paste)+"===null")&&managed.Input.Text==""&&Visuals<CheckBox>(managed.Page.PageVisual).Single().IsChecked!=true;managed.Page.Close();
            phase="fetch_busy";var pending=Page(url);var fetchReady=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            SubscriptionClient.HttpFixture=_=>new UpdateFixtureHandler28(async(_,token)=>{fetchReady.TrySetResult();await release.Task.WaitAsync(token);return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("socks5://127.0.0.1:9#Clipboard-fixture")};});
            await Click(Find(L.Text("IMPORT")));await fetchReady.Task.WaitAsync(TimeSpan.FromSeconds(3));
            checks["clipboard_buttons_disabled_during_subscription_fetch"]=await Wait(Native(pending.Paste)+"?.disabled===true");
            int busyReads=reads;pending.Paste.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));checks["busy_actions_do_not_read_or_overwrite_clipboard"]=reads==busyReads;
            release.TrySetResult();checks["clipboard_actions_reenabled_after_fetch_review"]=await Wait(Native(pending.Paste)+"?.disabled===false")&&data.Subscriptions.Count==0;pending.Page.Close();
            SubscriptionClient.HttpFixture=null;
            // Only replace the OS clipboard when it can be restored completely
            // from simple text formats. Images/custom app formats stay untouched.
            phase="native_clipboard";
            try{
                var original=Clipboard.GetDataObject();string[] formats=original?.GetFormats(false)??[];
                var allowed=new[]{DataFormats.UnicodeText,DataFormats.Text,DataFormats.OemText,DataFormats.StringFormat,DataFormats.Locale};
                if(formats.All(allowed.Contains)){
                    originalClipboard=new DataObject();bool complete=true;
                    foreach(string format in formats){var value=original!.GetData(format,false);if(value is string text)originalClipboard.SetData(format,text,false);else if(value is MemoryStream memory)originalClipboard.SetData(format,new MemoryStream(memory.ToArray()),false);else if(value is byte[] bytes)originalClipboard.SetData(format,bytes.ToArray(),false);else{complete=false;break;}}
                    if(complete){originalEmpty=formats.Length==0;replaceClipboard=true;Clipboard.SetText(url);subscriptionClipboardReadFixture33=null;var native=Page();await Click(Native(native.Paste));checks["native_windows_clipboard_paste"]=await Wait(Field(native.Input)+"?.value==="+JsonSerializer.Serialize(url));
                        checks["native_clipboard_unchanged_by_paste"]=Clipboard.GetText()==url;nativeRoundtrip=true;nativeScope="real text clipboard paste only, original restored in finally";native.Page.Close();
                    }else nativeScope="existing clipboard could not be fully captured; left untouched";
                }else nativeScope="existing non-text clipboard formats left untouched";
            }catch(System.Runtime.InteropServices.ExternalException){nativeScope="clipboard unavailable; synthetic pointer coverage retained";}
            checks["clipboard_actions_never_fetch_or_save_automatically"]=requests==0&&saves==0&&data.Subscriptions.Count==0;
            checks["network_agents_and_system_proxy_unchanged"]=client==null&&testing==null&&proxyBefore==new WinInetProxy().Read();
        }catch(Exception e){checks["exception_at_"+phase]=false;File.WriteAllText(report+".error",e.ToString());}
        finally{
            if(replaceClipboard){try{if(originalEmpty)Clipboard.Clear();else Clipboard.SetDataObject(originalClipboard!,true);checks["original_clipboard_restored"]=true;}catch{checks["original_clipboard_restored"]=false;}}
            foreach(var page in DialogWindow.OpenPages.Where(p=>p.PageHost==this).Reverse().ToArray())page.Close();subscriptionClipboardReadFixture33=null;SubscriptionClient.HttpFixture=null;storeSaveFixture28=null;htmlPageTimer29.Stop();durationTimer.Stop();routingTimer28.Stop();
        }
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,actualPointerClicks=pointers,nativeClipboardRoundtrip=nativeRoundtrip,nativeClipboardScope=nativeScope,originalClipboardContentRecorded=false,ownerStoreTouched=false,liveRemoteNetwork=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
