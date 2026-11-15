using System.IO;
using System.Text.Json;
using System.Windows;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunCardActivity30(string report)
    {
        var checks=new Dictionary<string,bool>();string phase="start",folder=Path.GetDirectoryName(report)!;Directory.CreateDirectory(folder);var proxy=new WinInetProxy().Read();const string source="activity30";
        try{
            storeSaveFixture28=()=>{};L.Language=data.Language="en";data.Theme="dark";Width=480;Height=900;ApplyTheme();
            var p=Importer.Single("hy2://fixture@127.0.0.1:19499#Activity");p.SourceId=source;p.CollectionId="sub:"+source;p.CollectionName="Activity";
            var other=Importer.Single("socks5://127.0.0.1:19498#Other");other.CollectionId="other30";data.Profiles=[p,other];data.Subscriptions=[new(){Id=source,Name="Activity",Url="https://feed.example.invalid/activity.txt"}];data.SelectedId=p.Id;data.OpenCollectionId=null;data.CollectionExpansionRevision=1;
            Build();await StartHtml29();Activate();html29!.Focus();
            string card="document.querySelector('[data-group=\"sub:activity30\"]')";
            string text=card+"?.querySelector('.collection-activity')?.textContent";
            string running=card+"?.querySelector('.collection-activity')?.dataset.busy";
            async Task<bool> Wait(string expression){for(int i=0;i<150;i++){if(htmlReady29&&await html29.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(4))=="true")return true;await Task.Delay(35);}return false;}
            async Task Click(string expression){if(!await Wait(expression+"!=null"))throw new InvalidOperationException("Missing control at "+phase);await html29.CoreWebView2.ExecuteScriptAsync("("+expression+").click()");}
            string Menu(string key)=>"[...document.querySelectorAll('.menu button')].find(b=>!b.closest('details:not([open])')&&b.getAttribute('aria-label')==="+JsonSerializer.Serialize(L.Text(key))+")";
            async Task Update(){await Click(card+".querySelector('.collection-top .actions button:last-child')");await Click(Menu("UPDATE"));}
            checks["closed_card_ready"]=await Wait(card+"!=null&&"+card+".querySelector('.profiles')===null");
            phase="update_success";var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var reply=new TaskCompletionSource<SubscriptionClient.Payload>(TaskCreationOptions.RunContinuationsAsynchronously);
            subscriptionFetchFixture28=(_,_,_)=>{ready.TrySetResult();return reply.Task;};await Update();await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            checks["updating_stays_on_closed_target_card_until_fetch_finishes"]=await Wait(text+"==="+JsonSerializer.Serialize(L.Text("SUBSCRIPTION_UPDATING"))+"&&"+running+"==='true'");
            await Task.Delay(700);Notify("Unrelated fixture notice");QueueHtml29();checks["updating_is_not_replaced_by_footer_or_telemetry"]=await Wait(text+"==="+JsonSerializer.Serialize(L.Text("SUBSCRIPTION_UPDATING")));
            await Click(card+".querySelector('.collection-top .actions button:first-child')");checks["updating_remains_when_card_is_opened"]=await Wait(card+".querySelector('.profiles')!==null&&"+running+"==='true'");
            await Click("document.querySelector('.flags button[aria-label=\"فارسی\"]')");checks["running_update_relabels_with_language"]=await Wait("document.documentElement.lang==='fa'&&"+text+"==="+JsonSerializer.Serialize(L.Text("SUBSCRIPTION_UPDATING")));
            reply.SetResult(new("hy2://fixture@127.0.0.1:19499#Updated",null,""));checks["successful_update_ends_with_card_completion"]=await Wait(text+"==="+JsonSerializer.Serialize(L.Text("SUBSCRIPTION_UPDATED"))+"&&"+running+"==='false'")&&!updatingSubscriptions20.Contains(source);
            checks["update_preserves_other_collection"]=data.Profiles.Any(x=>ReferenceEquals(x,other));
            phase="update_error";string before=JsonSerializer.Serialize(data.Profiles);ready=new(TaskCreationOptions.RunContinuationsAsynchronously);reply=new(TaskCreationOptions.RunContinuationsAsynchronously);subscriptionFetchFixture28=(_,_,_)=>{ready.TrySetResult();return reply.Task;};await Update();await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            checks["retry_shows_updating_instead_of_old_completion"]=await Wait(running+"==='true'");reply.SetException(new SafeError("FETCH_FAILED"));checks["failure_replaces_updating_and_preserves_profiles"]=await Wait(text+"==="+JsonSerializer.Serialize(L.Text("FETCH_FAILED"))+"&&"+running+"==='false'")&&before==JsonSerializer.Serialize(data.Profiles)&&CardActivity30(groups.Single(g=>g.Key=="sub:"+source))!.Ink==Red;
            phase="quick_test";var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource<QuickProbe30>(TaskCreationOptions.RunContinuationsAsynchronously);handshakeFixture30=(_,plan,stop)=>{gate.TrySetResult();return release.Task.WaitAsync(stop);};
            await Click(card+".querySelector('.collection-top .actions button:first-child')");await Click(card+".querySelector('[data-test=quick-ping]')");await gate.Task.WaitAsync(TimeSpan.FromSeconds(5));checks["quick_test_has_persistent_closed_card_activity"]=await Wait(running+"==='true'&&"+text+".includes('0/1')");
            release.SetResult(new(true,400,"","hysteria2-auth"));await collectionTask!.WaitAsync(TimeSpan.FromSeconds(5));checks["completed_quick_test_replaces_previous_update_failure"]=await Wait(running+"==='false'&&"+text+".includes("+JsonSerializer.Serialize(L.Text("GROUP_DONE"))+")")&&CardActivity30(groups.Single(g=>g.Key=="sub:"+source))!.Ink==Green;
            phase="cancel_test";gate=new(TaskCreationOptions.RunContinuationsAsynchronously);handshakeFixture30=async(_,plan,stop)=>{gate.TrySetResult();await Task.Delay(Timeout.Infinite,stop);return new(false,0,"CANCELLED",plan.Mechanism);};await Click(card+".querySelector('[data-test=quick-ping]')");await gate.Task.WaitAsync(TimeSpan.FromSeconds(5));await Click(card+".querySelector('[data-test=quick-ping]')");await collectionTask!.WaitAsync(TimeSpan.FromSeconds(5));checks["cancel_finishes_card_activity"]=await Wait(running+"==='false'&&"+text+".includes("+JsonSerializer.Serialize(L.Text("CANCELLED"))+")");
            phase="accurate_test";networkFixtureMode=true;gate=new(TaskCreationOptions.RunContinuationsAsynchronously);var https=new TaskCompletionSource<AgentReply>(TaskCreationOptions.RunContinuationsAsynchronously);collectionProbeFixture=(_,stop)=>{gate.TrySetResult();return https.Task.WaitAsync(stop);};await Click(card+".querySelector('.collection-top .actions button:last-child')");await Click(Menu("DETAILED_HTTPS"));await gate.Task.WaitAsync(TimeSpan.FromSeconds(5));checks["accurate_test_has_explicit_https_activity_until_completion"]=await Wait(running+"==='true'&&"+text+".includes("+JsonSerializer.Serialize(L.Text("DETAILED_HTTPS"))+")");https.SetResult(new(){Verified=true,ElapsedMs=450});await collectionTask!.WaitAsync(TimeSpan.FromSeconds(5));checks["accurate_completion_is_displayed_after_result_is_saved"]=await Wait(running+"==='false'&&"+text+".includes("+JsonSerializer.Serialize(L.Text("GROUP_DONE"))+")")&&data.Measurements.Values.Any(m=>m.Verified);networkFixtureMode=false;collectionProbeFixture=null;
            foreach(var language in new[]{"en","fa","ru","zh-CN"}){L.Language=data.Language=language;Build();QueueHtml29();checks["completed_activity_translates_"+language]=await Wait("document.documentElement.lang==="+JsonSerializer.Serialize(language)+"&&"+text+".includes("+JsonSerializer.Serialize(L.Text("GROUP_DONE"))+")");}
            using(var image=File.Create(Path.Combine(folder,"closed-card-activity.png")))await html29.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,image);
            checks["no_network_or_owner_store_changed"]=client==null&&testing==null&&proxy==new WinInetProxy().Read();
        }catch(Exception error){checks["exception_at_"+phase]=false;File.WriteAllText(report+".error",error.ToString());}
        finally{htmlPageTimer29.Stop();durationTimer.Stop();routingTimer28.Stop();subscriptionFetchFixture28=null;handshakeFixture30=null;storeSaveFixture28=null;networkFixtureMode=false;collectionProbeFixture=null;}
        bool passed=checks.Count>0&&checks.Values.All(x=>x);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,realHtmlCommands=true,delayedFetchAndProbeAdapters=true,liveRemoteNetwork=false,ownerStoreTouched=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
