using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    long routingEpoch28, routingLastCheck28, routingLastSwitch28;
    bool routingChecking28, routingSuppressed28, automaticConnecting28;
    int routingFailures28;
    CancellationTokenSource? routingStop28;
    readonly System.Windows.Threading.DispatcherTimer routingTimer28=new(){Interval=TimeSpan.FromSeconds(45)};
    Profile? connectionAttempt28;
    readonly Dictionary<string,int> routingCursor28=[];
    Func<Profile,CancellationToken,Task<AgentReply>>? routingProbeFixture28;
    Func<Profile,Task>? routingConnectFixture28;
    string RoutingMode28(Profile? p)=>p==null?"manual":RouteSelection28.Normalize(data.CollectionRoutingModes.GetValueOrDefault(CollectionKey(p)));
    void CancelRouting28(bool suppress=false)
    {
        routingEpoch28++;routingSuppressed28=suppress;routingFailures28=0;routingStop28?.Cancel();if(suppress)routingTimer28.Stop();
    }
    void SetRoutingMode28(string key,string mode)
    {
        mode=RouteSelection28.Normalize(mode);string? old=data.CollectionRoutingModes.GetValueOrDefault(key);
        if(old==mode)return;data.CollectionRoutingModes[key]=mode;
        try{Save();}catch{if(old==null)data.CollectionRoutingModes.Remove(key);else data.CollectionRoutingModes[key]=old;throw;}
        CancelRouting28();routingLastCheck28=0;RefreshRoutingTimer28();Notify(L.Text("SETTINGS_SAVED"));
    }
    void RefreshRoutingTimer28()
    {
        var origin=state.State=="connected"?activeProfile:connectionAttempt28??activeProfile;
        if(closing||routingSuppressed28||RoutingMode28(origin)=="manual"||state.State is not("connected" or "error"))routingTimer28.Stop();else routingTimer28.Start();
    }
    static bool RouteFailure28(string error)=>error is "AGENT_EXITED" or "CORE_EXITED" or "AGENT_FAILED" or "PROBE_PROXY_FAILED" or "PROBE_TIMEOUT" or "PROBE_DNS_FAILED" or "HTTPS_TEST_FAILED" or "DNS_TUNNEL_FAILED" or "SLIP_HANDSHAKE_FAILED" or "TEST_TIMEOUT";
    bool CandidateAllowed28(Profile p)
    {
        try{return p.CanConnect&&SupportPolicy.Allowed(p,data.IncludeExperimental)&&
            (!SshHostKey.Required(p)||StoredSshKey(p).Length>0)&&(!TransportPlan.UsesSlip(p.Raw)||StoredSlipPin(p).Length>0||CanAcquireSlipCertificate29(p))&&
            (!ChainProfiles.IsChain(p.Raw)||ChainProfiles.Read(p.Raw).Layers.All(CandidateAllowed28));}
        catch(SafeError){return false;}
    }
    async Task TickRouting28(bool force=false)
    {
        if(routingChecking28||routingSuppressed28||closing||reportingError||busy||currentCollection is {Running:true}||NavigationDepth>0||fixtureMode&&routingProbeFixture28==null)return;
        var origin=state.State=="connected"?activeProfile:connectionAttempt28??activeProfile;
        string mode=RoutingMode28(origin);if(origin==null||mode=="manual")return;
        bool failed=state.State=="error"&&RouteFailure28(state.Error.Length>0?state.Error:lastError)||state.State=="connected"&&routingFailures28>=2;
        bool currentFresh=state.State=="connected"&&liveLatencyTarget28==data.LatencyUrl&&liveLatency is >0&&latencySampled.HasValue&&(DateTimeOffset.UtcNow-latencySampled.Value).TotalSeconds<60;
        if(!failed&&(mode!="smart"||!currentFresh))return;
        if(!force&&(routingLastCheck28!=0&&Stopwatch.GetElapsedTime(routingLastCheck28).TotalSeconds<45||!failed&&routingLastSwitch28!=0&&Stopwatch.GetElapsedTime(routingLastSwitch28).TotalMinutes<2))return;
        string group=CollectionKey(origin),target=data.LatencyUrl;var candidates=data.Profiles.Where(p=>CollectionKey(p)==group&&p.Id!=origin.Id&&CollectionProbePlan.IsDirect(p)&&CandidateAllowed28(p)).OrderBy(p=>data.Measurements.GetValueOrDefault(p.Id)?.Verified==true?0:1).ThenBy(p=>data.Measurements.GetValueOrDefault(p.Id)?.ElapsedMs??long.MaxValue).ToArray();
        if(candidates.Length==0)return;
        routingChecking28=true;routingLastCheck28=Stopwatch.GetTimestamp();long epoch=routingEpoch28;var lease=routeGeneration.Capture();
        using var stop=new CancellationTokenSource();routingStop28=stop;
        bool Current()=>!stop.IsCancellationRequested&&!routingSuppressed28&&!closing&&epoch==routingEpoch28&&routeGeneration.IsCurrent(lease)&&RoutingMode28(origin)==mode&&!busy&&NavigationDepth==0&&data.LatencyUrl==target;
        try{
            // One bounded background candidate per tick. Cycle through the whole
            // collection without starting a second full group scan or moving rows.
            int cursor=routingCursor28.GetValueOrDefault(group)%candidates.Length;routingCursor28[group]=cursor+1;
            var candidate=candidates[cursor];string raw=candidate.Raw;
            var reply=routingProbeFixture28!=null?await routingProbeFixture28(candidate,stop.Token):await ProbeCollectionProfile(candidate,target,stop.Token,false);
            if(!Current()||!reply.Verified||reply.ElapsedMs<=0||reply.ProbeFallbackUsed||!CollectionProbePlan.IsDirect(candidate)||!CandidateAllowed28(candidate)||!data.Profiles.Any(p=>p.Id==candidate.Id&&p.Raw==raw&&CollectionKey(p)==group))return;
            measurements[candidate.Id]=reply;data.Measurements[candidate.Id]=new(){Verified=true,ElapsedMs=reply.ElapsedMs,Attempts=1,Passes=1,SampledAt=DateTimeOffset.UtcNow};liveMeasurements.Add(candidate.Id);
            // Compare the same HTTPS target and only a fresh active sample.
            bool fresh=liveLatencyTarget28==target&&latencySampled.HasValue&&(DateTimeOffset.UtcNow-latencySampled.Value).TotalSeconds<60;
            bool stillFailed=state.State=="error"&&RouteFailure28(state.Error.Length>0?state.Error:lastError)||state.State=="connected"&&routingFailures28>=2;
            if(!RouteSelection28.ShouldReplace(mode,stillFailed,fresh?liveLatency:null,reply.ElapsedMs)){RefreshCards();return;}
            if(!Current())return;
            automaticConnecting28=true;
            try{
                await Run(async()=>{
                    if(!CurrentWhileBusy())return;
                    string old=data.SelectedId;data.SelectedId=candidate.Id;try{Save();}catch{data.SelectedId=old;throw;}
                    if(client!=null)await Disconnect();
                    if(stop.IsCancellationRequested||epoch!=routingEpoch28||routingSuppressed28)return;
                    try{
                        await Connect();
                        if(state.State!="connected")throw new SafeError(state.Error.Length>0?state.Error:"AGENT_FAILED");
                    }catch(Exception error)when(!stillFailed&&!stop.IsCancellationRequested&&epoch==routingEpoch28&&!closing&&!routingSuppressed28&&
                        error is not SafeError{Code:"RECOVERY_REQUIRED" or "PROXY_RECOVERY_CONFLICT" or "PROXY_RECOVERY_FAILED"}&&
                        data.Profiles.Any(p=>p.Id==origin.Id&&p.Raw==origin.Raw)&&CandidateAllowed28(origin)){
                        // A verified candidate can fail at the real reconnect.
                        // Restore the healthy original once; never undo user stop.
                        if(client!=null)await Disconnect();
                        if(stop.IsCancellationRequested||epoch!=routingEpoch28||routingSuppressed28)return;
                        data.SelectedId=origin.Id;Save();await Connect();
                    }
                });
                if(state.State=="connected"&&activeProfile?.Id==candidate.Id){routingLastSwitch28=Stopwatch.GetTimestamp();routingFailures28=0;Notify(L.Text("ROUTING_SWITCHED")+" · "+candidate.Name);}
            }finally{automaticConnecting28=false;}
            bool CurrentWhileBusy()=>!stop.IsCancellationRequested&&!closing&&!routingSuppressed28&&epoch==routingEpoch28&&routeGeneration.IsCurrent(lease)&&RoutingMode28(origin)==mode&&NavigationDepth==0&&data.LatencyUrl==target;
        }catch(OperationCanceledException){}
        catch(SafeError error){if(!stop.IsCancellationRequested)Notify(L.Text(error.Code),MessageInk28(error.Code));}
        catch{if(!stop.IsCancellationRequested)Notify(L.Text("TEST_UNCONFIRMED"),Muted);}
        finally{if(ReferenceEquals(routingStop28,stop))routingStop28=null;routingChecking28=false;}
    }
    DialogWindow BuildRoutingPage28(List<Profile> members)
    {
        string key=CollectionKey(members[0]);var page=Dialog("PATH_SELECTION");var panel=new StackPanel{Margin=new(20)};
        foreach(string mode in new[]{"manual","failover","smart"}){
            string label="ROUTING_"+mode.ToUpperInvariant();var radio=new RadioButton{Content=L.Text(label),GroupName="collection-routing",IsChecked=RoutingMode28(members[0])==mode,MinHeight=44,Tag="routing:"+mode,FontSize=16};
            radio.Checked+=(_,_)=>{try{SetRoutingMode28(key,mode);}catch{radio.IsChecked=false;Notify(L.Text("SAVE_FAILED"),Red);foreach(var item in Visuals<RadioButton>(panel).Where(r=>r.GroupName=="collection-routing"))item.IsChecked=Equals(item.Tag,"routing:"+RoutingMode28(members[0]));}};
            panel.Children.Add(Card(new StackPanel{Children={radio,Text(L.Text(label+"_HINT"),12,Muted)}}));
        }
        panel.Children.Add(Text(L.Text("ROUTING_DIRECT_ONLY_HINT"),13,Muted));
        panel.Children.Add(Text(L.Text("SELECT_PATH"),14,Muted));
        foreach(var profile in members){var button=SettingsAction("SELECT",()=>{if(busy)return;SelectProfile20(profile);page.DialogResult=true;});button.Content=profile.Name+" · "+profile.Protocol;button.IsEnabled=profile.CanConnect;panel.Children.Add(Card(button));}
        page.Content=panel;return page;
    }
}
