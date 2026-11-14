using System.Globalization;
using System.Windows;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    readonly HashSet<string> livePings29=[];
    Func<Profile,HandshakePlan30,CancellationToken,Task<QuickProbe30>>? handshakeFixture30;
    readonly SemaphoreSlim handshakeSlots30=new(2,2);
    async Task<QuickProbe30> QuickProbeProfile30(Profile p,CancellationToken stop)
    {
        var plan=ProtocolHandshake30.Plan(p);
        if(plan==null){long? tcp=await CollectionActions.Tcp(p,stop);return new(tcp.HasValue,tcp??0,tcp.HasValue?"":"TCP_NOT_APPLICABLE","tcp");}
        if(state.State=="connected"&&ProtocolHandshake30.Conflicts(plan,activeProfile))return new(false,0,"HANDSHAKE_ACTIVE_CONFLICT",plan.Mechanism);
        await handshakeSlots30.WaitAsync(stop);
        try{return fixtureMode&&handshakeFixture30!=null?await handshakeFixture30(p,plan,stop):await ProtocolHandshakeClient30.Probe(plan,stop);}finally{handshakeSlots30.Release();}
    }
    void QuickPing29(List<Profile>? profiles=null)=>StartCollectionTest(profiles,1,true);
    bool HasCurrentPing29(Profile p,out PingMeasurement ping)
    {
        if(data.PingMeasurements.TryGetValue(p.Id,out var stored)&&stored.ProfileFingerprint==Importer.Fingerprint(p.Raw)){ping=stored;return true;}
        ping=null!;return false;
    }
    bool PreferPing29(Profile p,out PingMeasurement ping)=>HasCurrentPing29(p,out ping)&&
        (!data.Measurements.TryGetValue(p.Id,out var https)||ping.SampledAt>=https.SampledAt);

    async Task RunQuickPing29(List<Profile> members)
    {
        // TCP stays a bare socket. UDP protocols use an isolated, authenticated
        // handshake helper; no external HTTPS request or system proxy mutation.
        var plan=new CollectionProbePlan(members);
        var job=new CollectionRun{Key=CollectionKey(members[0]),Tcp=true,Running=true,Total=plan.Direct.Length,ExcludedDns=plan.Dns.Length};
        collectionRuns[job.Key]=currentCollection=job;var prior=new Dictionary<string,PingMeasurement>(data.PingMeasurements);var priorLive=new HashSet<string>(livePings29);
        foreach(var p in plan.Direct)job.Pending[p.Id]="GROUP_WAIT_DIRECT";foreach(var p in plan.Dns)job.Pending[p.Id]="QUICK_DNS_EXCLUDED";
        using var stop=new CancellationTokenSource();testStop=stop;var token=stop.Token;RefreshCards();
        try{
            async Task Test(Profile p){
                string fingerprint=Importer.Fingerprint(p.Raw);job.Pending[p.Id]="GROUP_TESTING";job.Current=p.Name;RefreshCards();AgentReply reply;string mechanism="tcp";
                try{var result=await QuickProbeProfile30(p,token);mechanism=result.Mechanism;reply=new(){ElapsedMs=result.ElapsedMs,Error=result.Error,ProbeStage=mechanism};}
                catch(OperationCanceledException){if(token.IsCancellationRequested)return;reply=new(){Error="PING_TIMEOUT"};}
                catch(System.Net.Sockets.SocketException){reply=new(){Error="PING_FAILED"};}
                catch(SafeError e){reply=new(){Error=e.Code};}
                catch{reply=new(){Error="HANDSHAKE_FAILED"};}
                if(token.IsCancellationRequested)return;
                job.Pending.Remove(p.Id);job.Completed++;
                bool current=data.Profiles.Any(current=>current.Id==p.Id&&Importer.Fingerprint(current.Raw)==fingerprint);
                if(!current){job.Pending[p.Id]="RESULT_STALE";RefreshCards();return;}
                if(reply.Error=="HANDSHAKE_ACTIVE_CONFLICT"){job.Pending[p.Id]=reply.Error;RefreshCards();return;}
                job.Results[p.Id]=reply;if(reply.Error.Length==0)job.Passed++;
                if(current){
                    data.PingMeasurements[p.Id]=new(){Mechanism=mechanism,Reachable=reply.Error.Length==0,ElapsedMs=reply.ElapsedMs,Error=reply.Error,ProfileFingerprint=fingerprint,SampledAt=DateTimeOffset.UtcNow};livePings29.Add(p.Id);
                }
                RefreshCards();
            }
            async Task Phase(Profile[] profiles){
                // Four sockets maximum, each with a three-second DNS/connect cap.
                using var slots=new SemaphoreSlim(4);
                await Task.WhenAll(profiles.Select(async p=>{await slots.WaitAsync(token);try{await Test(p);}finally{slots.Release();}}));
            }
            await Phase(plan.Direct);
        }catch(OperationCanceledException){}
        catch{job.Error="PING_FAILED";}
        finally{
            job.Cancelled=token.IsCancellationRequested;job.Running=false;job.Current="";
            foreach(var id in job.Pending.Where(p=>p.Value is not("QUICK_DNS_EXCLUDED" or "RESULT_STALE" or "HANDSHAKE_ACTIVE_CONFLICT")).Select(p=>p.Key).ToArray())job.Pending[id]=job.Cancelled?"CANCELLED":"GROUP_NOT_TESTED";
            if(ReferenceEquals(testStop,stop))testStop=null;
            try{Save();}catch{data.PingMeasurements=prior;livePings29.Clear();livePings29.UnionWith(priorLive);job.Error="SAVE_FAILED";job.Results.Clear();job.Passed=0;foreach(var p in plan.Direct)job.Pending[p.Id]="SAVE_FAILED";}
            job.FinishedAt30=DateTimeOffset.UtcNow;
            RefreshCards();
        }
    }
    void RenderPing29(RowView row)
    {
        if(collectionRuns.TryGetValue(CollectionKey(row.Profile),out var job)&&job.Pending.TryGetValue(row.Profile.Id,out var pending)&&(job.Running||pending is "QUICK_DNS_EXCLUDED" or "RESULT_STALE" or "HANDSHAKE_ACTIVE_CONFLICT" or "SAVE_FAILED"))return;
        if(!PreferPing29(row.Profile,out var ping))return;
        if(state.State=="connected"&&activeProfile?.Id==row.Profile.Id&&latencySampled.HasValue&&latencySampled.Value>ping.SampledAt&&liveLatency.HasValue)return;
        bool fresh=livePings29.Contains(row.Profile.Id)&&UiPolicy.MeasurementFresh(ping.SampledAt,DateTimeOffset.UtcNow);
        row.Timing.Visibility=row.Result.Visibility=Visibility.Visible;
        row.Timing.Text=ping.Reachable?ping.ElapsedMs.ToString(CultureInfo.InvariantCulture)+" ms":"—";
        string success=ping.Mechanism=="tuic-authenticated-relay"?"HANDSHAKE_RELAY_OK":ping.Mechanism is not("" or "tcp")?"HANDSHAKE_OK":"PING_REPLIED";
        row.Result.Text=L.Text(ping.Reachable?success:ping.Error.Length>0?ping.Error:"PING_FAILED")+(ping.Reachable&&!ProtocolHandshake30.GreenLatency(ping.ElapsedMs)?" · "+L.Text("PING_SLOW"):"")+(!fresh?" · "+L.Text("RESULT_STALE"):"");
        row.Result.Foreground=row.Timing.Foreground=ping.Reachable?fresh?ProtocolHandshake30.GreenLatency(ping.ElapsedMs)?Green:Amber:Muted:ping.Error=="TCP_NOT_APPLICABLE"?Muted:ping.Error=="HANDSHAKE_UNCONFIRMED"?Amber:Red;
        row.Timing.ToolTip=ping.SampledAt.ToLocalTime().ToString("g");row.Result.ToolTip=row.Result.Text+"\n"+L.Text(ping.Mechanism is not("" or "tcp")?"HANDSHAKE_SCOPE":"PING_SCOPE");
    }
}
