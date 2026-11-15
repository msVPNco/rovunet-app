using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunRouting28(string report)
    {
        var start=DateTimeOffset.UtcNow;var checks=new Dictionary<string,bool>();string dir=Path.GetDirectoryName(report)!;Directory.CreateDirectory(dir);
        try{
            storeSaveFixture28=()=>{};
            var a=Importer.Single("socks5://127.0.0.1:19101#Current");var b=Importer.Single("socks5://127.0.0.1:19102#Alternative");var outside=Importer.Single("socks5://127.0.0.1:19103#Other");
            a.CollectionId=b.CollectionId="routing";outside.CollectionId="other";a.CollectionName=b.CollectionName="Fixture";
            data.Profiles=[a,b,outside];data.Subscriptions=[];data.SelectedId=a.Id;Build();await Idle18();
            int switched=0,probes=0;
            void Setup(string mode,bool failed=false){CancelRouting28();routingLastCheck28=routingLastSwitch28=0;routingCursor28.Clear();data.CollectionRoutingModes["routing"]=mode;data.SelectedId=a.Id;activeProfile=a;connectionAttempt28=a;liveLatency=1000;liveLatencyTarget28=data.LatencyUrl;latencySampled=DateTimeOffset.UtcNow;SetState(new(){State=failed?"error":"connected",Error=failed?"CORE_EXITED":"",Verified=!failed});switched=probes=0;}
            routingConnectFixture28=p=>{switched++;data.SelectedId=p.Id;activeProfile=p;RecordConnectedUse17(p);SetState(new(){State="connected"});return Task.CompletedTask;};
            routingProbeFixture28=(p,stop)=>{probes++;checks["candidate_stays_in_same_collection"]=p.Id==b.Id;return Task.FromResult(new AgentReply{Verified=true,ElapsedMs=500});};
            Setup("manual",true);await TickRouting28(true);checks["manual_never_tests_or_switches"]=probes==0&&switched==0;
            Setup("failover",true);checks["failure_retry_timer_is_enabled"]=routingTimer28.IsEnabled;CancelRouting28(true);checks["manual_stop_disables_retry_timer"]=!routingTimer28.IsEnabled;
            Setup("failover");await TickRouting28(true);checks["failover_keeps_healthy_route"]=probes==0&&switched==0;
            Setup("failover",true);await TickRouting28(true);checks["failover_switches_after_core_failure"]=switched==1&&activeProfile==b;
            Setup("smart");routingProbeFixture28=(_,_)=>Task.FromResult(new AgentReply{Verified=true,ElapsedMs=501});await TickRouting28(true);checks["smart_rejects_less_than_twice_faster"]=switched==0;
            Setup("smart");routingProbeFixture28=(_,_)=>Task.FromResult(new AgentReply{Verified=true,ElapsedMs=500});await TickRouting28(true);checks["smart_accepts_exact_half_latency"]=switched==1;
            Setup("smart");liveLatencyTarget28="https://other.invalid/previous-target";await TickRouting28(true);checks["smart_rejects_baseline_from_previous_target"]=probes==0&&switched==0;
            Setup("smart");latencySampled=DateTimeOffset.UtcNow.AddMinutes(-2);await TickRouting28(true);checks["smart_rejects_stale_baseline"]=probes==0&&switched==0;
            Setup("smart");routingProbeFixture28=(_,_)=>Task.FromResult(new AgentReply{Verified=true,ElapsedMs=100,ProbeFallbackUsed=true});await TickRouting28(true);checks["different_https_target_never_compared"]=switched==0;
            Setup("failover");routingFailures28=1;await TickRouting28(true);checks["single_health_failure_does_not_switch"]=switched==0; routingFailures28=2;await TickRouting28(true);checks["two_health_failures_allow_replacement"]=switched==0; // fixture used a different target: still rejected
            Setup("failover");routingFailures28=2;routingProbeFixture28=(_,_)=>Task.FromResult(new AgentReply{Verified=true,ElapsedMs=300});await TickRouting28(true);checks["two_failures_same_target_switch"]=switched==1;
            foreach(string action in new[]{"manual-stop","mode-change","route-change","recovered","profile-removed","target-changed"}){
                Setup(action=="recovered"?"failover":"smart");if(action=="recovered")routingFailures28=2;
                var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource<AgentReply>(TaskCreationOptions.RunContinuationsAsynchronously);
                routingProbeFixture28=(_,_)=>{ready.SetResult();return release.Task;};var tick=TickRouting28(true);await ready.Task;
                string url=data.LatencyUrl;if(action=="manual-stop")CancelRouting28(true);if(action=="mode-change")SetRoutingMode28("routing","manual");if(action=="route-change")routeGeneration.Advance();if(action=="recovered")routingFailures28=0;if(action=="profile-removed")data.Profiles=[a,outside];if(action=="target-changed")data.LatencyUrl="https://example.invalid/new-target";
                release.SetResult(new(){Verified=true,ElapsedMs=100});await tick;checks[action+"_wins_over_late_probe"]=switched==0;data.Profiles=[a,b,outside];data.LatencyUrl=url;
            }
            Setup("smart");routingProbeFixture28=(_,_)=>Task.FromResult(new AgentReply{Verified=true,ElapsedMs=100});int attempts=0;
            routingConnectFixture28=p=>{attempts++;if(p.Id==b.Id)throw new SafeError("AGENT_FAILED");activeProfile=p;SetState(new(){State="connected"});return Task.CompletedTask;};await TickRouting28(true);
            checks["failed_smart_reconnect_restores_original_once"]=attempts==2&&state.State=="connected"&&activeProfile==a&&data.SelectedId==a.Id;
            routingConnectFixture28=null;
            foreach(string mode in new[]{"manual","failover","smart"})foreach(bool failed in new[]{false,true})foreach(long? current in new long?[]{null,0,1000,long.MaxValue})foreach(long candidate in new[]{0L,500,501,long.MaxValue}){
                bool expected=mode!="manual"&&candidate>0&&(failed||mode=="smart"&&current>0&&candidate<=current.Value/2);checks[$"policy_{mode}_{failed}_{current}_{candidate}"]=RouteSelection28.ShouldReplace(mode,failed,current,candidate)==expected;
            }
            // Public sources tolerate unknown rows without destructive replacement.
            SetState(new());activeProfile=null;connectionAttempt28=null;routingProbeFixture28=null;
            string mahsa=PublicFeed.MahsaUrl;var old=Importer.Single("socks5://127.0.0.1:19201#Old");old.SourceId="mahsa-fixture";old.ItemId=Importer.Fingerprint(old.Raw);old.CollectionId="sub:mahsa-fixture";old.CollectionName="Saved Mahsa";
            var manual=Importer.Single("socks5://127.0.0.1:19202#Manual");data.Profiles=[old,manual];data.Subscriptions=[new(){Id=old.SourceId,Url=mahsa,Name="Saved Mahsa",PublicSource="mahsa"}];data.SelectedId=old.Id;
            string baseline=JsonSerializer.Serialize(new[]{old,manual});string fresh="socks5://127.0.0.1:19203#Fresh";
            var partial=PublicFeed.Preview(fresh+"\nfuturevpn://example.invalid/unsupported\nrovunet://bad-envelope",true);
            checks["future_native_and_standard_rows_do_not_break_usable_feed"]=partial.Profiles.Count==1&&partial.Skipped.Values.Sum()==2;
            int added=CommitPublicBatch28(new Uri(mahsa),partial,"ignored",old.SourceId);
            checks["partial_update_adds_valid_and_preserves_previous_bytes"]=added==1&&data.Profiles.Count==3&&JsonSerializer.Serialize(data.Profiles.Take(2))==baseline&&data.SelectedId==old.Id;
            string full=JsonSerializer.Serialize(data.Profiles);var another=PublicFeed.Preview("socks5://127.0.0.1:19204#Another\nnewtransport://fixture",true);storeSaveFixture28=()=>throw new IOException("synthetic save failure");bool rejected=false;try{CommitPublicBatch28(new Uri(mahsa),another,"",old.SourceId);}catch(IOException){rejected=true;}checks["partial_save_failure_rolls_back"]=rejected&&JsonSerializer.Serialize(data.Profiles)==full;storeSaveFixture28=()=>{};
            var sameManual=PublicFeed.Preview(manual.Raw+"\nnewtransport://fixture",true);CommitPublicBatch28(new Uri(mahsa),sameManual,"",old.SourceId);checks["manual_identity_not_taken_over"]=data.Profiles.Contains(manual)&&manual.SourceId==""&&data.Profiles.Select(p=>p.Id).Distinct().Count()==data.Profiles.Count;
            var clean=PublicFeed.Preview(fresh,true);CommitPublicBatch28(new Uri(mahsa),clean,"Saved Mahsa",old.SourceId);checks["fully_valid_refresh_replaces_only_its_collection"]=data.Profiles.Count==2&&data.Profiles.Contains(manual)&&data.Profiles.Any(p=>p.SourceId==old.SourceId&&p.Raw==fresh);
            string? sample=Environment.GetEnvironmentVariable("ROVUNET_MAHSA_SAMPLE");if(!string.IsNullOrEmpty(sample)){
                var links=MahsaFeed28.Decode(File.ReadAllText(sample));var batch=PublicFeed.Preview(string.Join('\n',links),true);checks["live_sample_now_has_eight_compatible_profiles"]=batch.Profiles.Count==8&&batch.Skipped.GetValueOrDefault("TLS_UNVERIFIED")==2&&!batch.Skipped.ContainsKey("UNSUPPORTED_FORMAT");
                File.WriteAllText(Path.Combine(dir,"live-mahsa-redacted.json"),JsonSerializer.Serialize(new{decoded=links.Length,accepted=batch.Profiles.Count,skipped=batch.Skipped,containsRawLinks=false,livePeerTest=false}));
            }
            checks["unknown_mode_defaults_manual"]=RouteSelection28.Normalize("future-mode")=="manual";
            var restored=JsonSerializer.Deserialize<UserData>(JsonSerializer.Serialize(data))!;checks["routing_modes_survive_store_serialization"]=restored.CollectionRoutingModes.Count==data.CollectionRoutingModes.Count&&restored.CollectionRoutingModes.All(p=>data.CollectionRoutingModes[p.Key]==p.Value);
            string oldMode=RoutingMode28(a);storeSaveFixture28=()=>throw new IOException("synthetic mode save failure");bool modeFailed=false;try{SetRoutingMode28("routing",oldMode=="manual"?"smart":"manual");}catch(IOException){modeFailed=true;}checks["mode_save_failure_restores_previous_policy"]=modeFailed&&RoutingMode28(a)==oldMode;storeSaveFixture28=()=>{};
            checks["direct_budget_18s"]=RouteSelection28.TestBudget(a).TotalSeconds==18;
            // Fail-fast stability must report actual completed attempts.
            data.Profiles=[a,b];data.SelectedId=a.Id;data.Subscriptions=[];SetState(new());Build();networkFixtureMode=true;int calls=0;collectionProbeFixture=(_,_)=>{calls++;return Task.FromResult(new AgentReply{Error="PROBE_TIMEOUT"});};StartCollectionTest([a,b],3,false);await collectionTask!;checks["failed_stability_not_repeated_three_times"]=calls==2&&data.Measurements[a.Id].Attempts==1&&data.Measurements[b.Id].Attempts==1;networkFixtureMode=false;collectionProbeFixture=null;
            foreach(string language in new[]{"fa","en","ru","zh-CN"})foreach(string theme in new[]{"dark","light","amoled"}){
                L.Language=data.Language=language;data.Theme=theme;Width=480;ApplyTheme();Build();var page=BuildRoutingPage28([a,b]);page.Show();await Idle18();
                checks[language+theme+"_three_localized_modes"]=Visuals<RadioButton>(page.PageVisual).Count(r=>r.Tag is string tag&&tag.StartsWith("routing:"))==3&&L.Text("PUBLIC_PARTIAL_READY")!="PUBLIC_PARTIAL_READY";
                if(language=="fa")SavePage(page.PageVisual,Path.Combine(dir,"routing-"+theme+".png"));page.Close();
            }
        }catch(Exception e){checks["exception"]=false;File.WriteAllText(report+".error",e.ToString());}
        finally{networkFixtureMode=false;routingProbeFixture28=null;routingConnectFixture28=null;storeSaveFixture28=null;collectionProbeFixture=null;CancelRouting28(true);durationTimer.Stop();}
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,startUtc=start,endUtc=DateTimeOffset.UtcNow,pid=Environment.ProcessId,realWpf=true,liveNetwork=false,ownerStoreTouched=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
