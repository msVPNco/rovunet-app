using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;
using Rovunet.Platform;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunScanStart32(string report)
    {
        string folder=Path.GetDirectoryName(report)!,phase="start";Directory.CreateDirectory(folder);
        var checks=new Dictionary<string,bool>();int pointers=0,fetches=0,verifies=0;var proxyBefore=new WinInetProxy().Read();
        using var sampleStream=typeof(MainWindow).Assembly.GetManifestResourceStream("Rovunet.SlipTrust29.Sample")!;using var reader=new StreamReader(sampleStream);string sample=reader.ReadToEnd();var trust=JsonNode.Parse(sample)!;
        bool fetching=false,cancelled=false;TaskCompletionSource gate=new(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Wait(string expression){for(int i=0;i<150;i++){if(htmlReady29&&await html29!.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(4))=="true")return true;await Task.Delay(30);}return false;}
        string Find(string label)=>"[...document.querySelectorAll('.native-page button')].find(b=>b.getClientRects().length&&b.getAttribute('aria-label')==="+JsonSerializer.Serialize(label)+")";
        async Task Click(string expression){
            if(!await Wait(expression+"!=null"))throw new InvalidOperationException("Missing "+phase);
            await html29!.CoreWebView2.ExecuteScriptAsync("("+expression+").scrollIntoView({block:'center',inline:'nearest'})");await Task.Delay(35);
            using var point=JsonDocument.Parse(await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const b="+expression+",r=b.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return{x,y,hit:b.contains(document.elementFromPoint(x,y)),disabled:b.disabled===true}})()"));var p=point.RootElement;
            if(!p.GetProperty("hit").GetBoolean()||p.GetProperty("disabled").GetBoolean())throw new InvalidOperationException("Obscured "+phase);
            foreach(string type in new[]{"mousePressed","mouseReleased"})await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type,x=p.GetProperty("x").GetDouble(),y=p.GetProperty("y").GetDouble(),button="left",clickCount=1}));pointers++;
        }
        (Profile Profile,DialogWindow Page,ObservableCollection<ScanEntry> Entries) Page(string kind,string mode){
            var profile=Importer.Single(ManualProfiles.Dns(kind,phase,"slip.example.test","127.0.0.1:9,127.0.0.2:9","udp","",kind.EndsWith("_ssh")?"ssh.example.test":"",kind.EndsWith("_ssh")?"22":"",kind.EndsWith("_ssh")?"synthetic-user":"",kind.EndsWith("_ssh")?"synthetic-password":""));
            profile.SourceId="scanstart32";profile.ItemId="slipstream-socks";data.Profiles=[profile];data.SelectedId=profile.Id;
            data.Subscriptions=[new(){Id=profile.SourceId,Name="Synthetic",Url="https://panel.example.test/sub/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.windows.json"}];
            statuses[profile.SourceId]=new(){Status=new(){ServerId=trust["server_id"]!.ToString(),AccountId=trust["account_id"]!.ToString(),PeriodId=trust["period_id"]!.ToString()}};
            data.SlipTrustPins.Clear();data.ResolverOptions[profile.Id]=new(){Mode=mode,Source="profile",Transport="udp",Shuffle=false,Limit=2,FullVerification=false};
            BuildCards();var page=BuildResolverFinder(profile);page.Show();QueueHtml29();return(profile,page,(ObservableCollection<ScanEntry>)Visuals<ListBox>(page.PageVisual).Single().ItemsSource);
        }
        try{
            storeSaveFixture28=()=>{};L.Language=data.Language="en";data.Theme="dark";Width=480;Height=900;ApplyTheme();Build();await StartHtml29();Activate();html29!.Focus();
            scannerWorkerFixture32=async(request,receive,token)=>{
                await Task.Delay(30,token);var node=JsonSerializer.SerializeToNode(request)!;int count=node["Candidates"]!.AsArray().Count;
                foreach(string method in node["Transports"]!.AsArray().Select(v=>v!.GetValue<string>()))for(int i=0;i<count;i++){token.ThrowIfCancellationRequested();await receive(new(){Index=i,Transport=method,Dns=true,Pass=true,DnsElapsedMS=5,ElapsedMS=5,Completed=2,Checks=["TXT","NS"]});}
            };
            scannerVerifyFixture32=(_,_)=>{verifies++;return Task.FromResult(new AgentReply{State="connected",TunnelEstablished=true,ElapsedMs=17});};
            foreach(string language in new[]{"en","fa","ru","zh-CN"})foreach(string theme in new[]{"light","dark","amoled"})foreach(string kind in new[]{"slipstream","slipstream_ssh"})foreach(string mode in new[]{"simple","e2e"}){
                phase=language+"_"+theme+"_"+kind+"_"+mode;L.Language=data.Language=language;data.Theme=theme;ApplyTheme();Build();fetches=verifies=0;fetching=cancelled=false;gate=new(TaskCreationOptions.RunContinuationsAsynchronously);
                certificateFetchFixture29=async(_,token)=>{fetches++;fetching=true;try{await gate.Task.WaitAsync(token);return sample;}catch(OperationCanceledException){cancelled=true;throw;}finally{fetching=false;}};
                var current=Page(kind,mode);await Click(Find(L.Text("START_TEST")));
                checks[phase+"_results_open_while_certificate_fetch_is_pending"]=await Wait("document.querySelector('.page-title')?.textContent==="+JsonSerializer.Serialize(L.Text("SCAN_RESULTS")))&&fetching&&fetches==1;
                checks[phase+"_pending_stage_is_localized_and_stop_is_visible"]=await Wait("document.querySelector('.native-page')?.textContent.includes("+JsonSerializer.Serialize(L.Text("SCAN_PREPARING_CERTIFICATE"))+")&&"+Find(L.Text("SCAN_STOP_TUNNEL"))+"!=null");
                var clock=Stopwatch.StartNew();await Click(Find(L.Text("SCAN_STOP_TUNNEL")));
                checks[phase+"_stop_cancels_fetch_without_waiting_for_network_timeout"]=await Wait(Find(L.Text("SCAN_RESUME_TUNNEL"))+"!=null")&&cancelled&&!fetching&&clock.Elapsed<TimeSpan.FromSeconds(3);
                checks[phase+"_cancel_does_not_create_certificate_or_mark_resolvers_failed"]=data.SlipTrustPins.Count==0&&verifies==0&&current.Entries.Count==2&&current.Entries.All(e=>!e.TunnelFinished&&!e.Testing&&!e.Pending&&e.TunnelError.Length==0);
                await Click(Find(L.Text("BACK")));await Click(Find(L.Text("BACK")));
            }
            // Fail after DNS has finished: this used to escape Observe's one-time
            // error refresh. It must be a single global setup error, not a
            // measured failure for every resolver; retry must keep the DNS proofs.
            L.Language=data.Language="en";data.Theme="dark";ApplyTheme();Build();
            foreach(string kind in new[]{"slipstream","slipstream_ssh"})foreach(string mode in new[]{"simple","e2e"}){
                phase="retry_"+kind+"_"+mode;fetches=verifies=0;gate=new(TaskCreationOptions.RunContinuationsAsynchronously);
                string errorCode=mode=="e2e"?"SLIP_TRUST_NOT_FOUND":"SLIP_TRUST_FETCH_FAILED";
                SubscriptionClient.HttpFixture=_=>new UpdateFixtureHandler28((_,_)=>Task.FromResult(new System.Net.Http.HttpResponseMessage(mode=="e2e"?System.Net.HttpStatusCode.NotFound:System.Net.HttpStatusCode.ServiceUnavailable)));
                certificateFetchFixture29=async(uri,token)=>{fetches++;await gate.Task.WaitAsync(token);return(await SubscriptionClient.Fetch(uri,cancellation:token)).Body;};
                var current=Page(kind,mode);await Click(Find(L.Text("START_TEST")));await Wait(Find(L.Text("SCAN_STOP_TUNNEL"))+"!=null");await Task.Delay(140);gate.TrySetResult();
                checks[phase+"_late_http_setup_failure_remains_visible_in_red"]=await Wait("document.querySelector('.native-page')?.textContent.includes("+JsonSerializer.Serialize(L.Text(errorCode))+")")&&NavigationDepth==1&&fetches==1&&verifies==0&&Visuals<TextBlock>(current.Page.PageVisual).Any(t=>t.Text==L.Text(errorCode)&&t.Foreground==Red);
                checks[phase+"_failed_setup_is_resumable_without_false_tunnel_failures"]=current.Entries.Count==2&&current.Entries.All(e=>!e.TunnelFinished&&e.TunnelError.Length==0)&&(mode=="e2e"||current.Entries.All(e=>e.Result.Pass));
                certificateFetchFixture29=(_,_)=>{fetches++;return Task.FromResult(sample);};await Click(Find(L.Text("SCAN_RESUME_TUNNEL")));
                checks[phase+"_retry_fetches_once_and_tests_each_resolver"]=await Wait(Find(L.Text("SCAN_RETEST_TUNNEL"))+"!=null")&&fetches==2&&verifies==2&&data.SlipTrustPins.Count==1&&current.Entries.All(e=>e.TunnelFinished&&e.TunnelPassed&&e.TunnelError.Length==0);
                checks[phase+"_simple_keeps_dns_stage_and_e2e_reports_actual_tunnel_count"]=Visuals<TextBlock>(current.Page.PageVisual).Single(t=>Equals(t.Tag,"SCAN_TOTAL")).Text=="2"&&Visuals<TextBlock>(current.Page.PageVisual).Single(t=>Equals(t.Tag,"SCAN_WORKING")).Text=="2";
                using(var shot=File.Create(Path.Combine(folder,phase+".png")))await html29.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,shot);
                await Click(Find(L.Text("BACK")));await Click(Find(L.Text("BACK")));
            }
            SubscriptionClient.HttpFixture=null;
            foreach(string kind in new[]{"slipstream","slipstream_ssh"})foreach(string mode in new[]{"simple","e2e"})foreach(bool close in new[]{false,true}){
                phase=(close?"close_":"back_")+kind+"_"+mode;fetching=cancelled=false;fetches=verifies=0;gate=new(TaskCreationOptions.RunContinuationsAsynchronously);
                certificateFetchFixture29=async(_,token)=>{fetches++;fetching=true;try{await gate.Task.WaitAsync(token);return sample;}catch(OperationCanceledException){cancelled=true;throw;}finally{fetching=false;}};
                var current=Page(kind,mode);await Click(Find(L.Text("START_TEST")));await Wait(Find(L.Text("SCAN_STOP_TUNNEL"))+"!=null");
                if(close)current.Page.Close();else await Click(Find(L.Text("BACK")));
                checks[phase+"_pending_acquisition_is_cancelled_on_departure"]=await Wait(close?"document.querySelector('.hero')!==null":"document.querySelector('.page-title')?.textContent==="+JsonSerializer.Serialize(L.Text("DNS_SCANNER")))&&cancelled&&!fetching&&data.SlipTrustPins.Count==0&&verifies==0;
                checks[phase+"_no_false_completed_tests_are_saved"]=current.Entries.All(e=>!e.TunnelFinished&&e.TunnelError.Length==0);
                if(!close)await Click(Find(L.Text("BACK")));
            }
            checks["no_live_slipstream_or_owner_network_changes"]=proxyBefore==new WinInetProxy().Read()&&client==null&&testing==null;
        }catch(Exception e){checks["exception_at_"+phase]=false;File.WriteAllText(report+".error",e.ToString());}
        finally{gate.TrySetCanceled();foreach(var page in DialogWindow.OpenPages.Where(p=>p.PageHost==this).Reverse().ToArray())page.Close();SubscriptionClient.HttpFixture=null;certificateFetchFixture29=null;scannerWorkerFixture32=null;scannerVerifyFixture32=null;storeSaveFixture28=null;htmlPageTimer29.Stop();durationTimer.Stop();routingTimer28.Stop();}
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,actualPointerClicks=pointers,syntheticCertificateAndDns=true,realNetwork=false,ownerStoreTouched=false,scope="Slipstream SOCKS/SSH Simple/E2E, four languages, three themes, delayed certificate, stop, late failure, retry"},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
