using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Rovunet.Core;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    sealed class CollectionRun
    {
        internal required string Key;
        internal bool Running,Tcp,Cancelled;
        internal string Phase="GROUP_DIRECT",Current="",Error="";
        internal int Total,Completed,Passed,ExcludedDns;
        internal DateTimeOffset FinishedAt30;
        internal readonly Dictionary<string,string> Pending=[];
        internal readonly Dictionary<string,AgentReply> Results=[];
    }
    readonly Dictionary<string,CollectionRun> collectionRuns=[];
    CollectionRun? currentCollection;
    Task? collectionTask;
    readonly HashSet<string> liveMeasurements=[];
    bool LatestRound(Profile p,Rovunet.Platform.PathMeasurement m)=>!data.CollectionTestStarted.TryGetValue(CollectionKey(p),out var start)||m.SampledAt>=start;
    Func<Profile,CancellationToken,Task<AgentReply>>? collectionProbeFixture;
    static string CollectionKey(Profile p)=>p.SourceId.Length>0?"sub:"+p.SourceId:p.CollectionId;
    void CancelCollectionTest(){testStop?.Cancel();testing?.Abort();RefreshCards();}
    static Dictionary<string,long> PassedTimes(CollectionRun job)=>job.Results.Where(x=>job.Tcp?x.Value.Error.Length==0:x.Value.Verified).ToDictionary(x=>x.Key,x=>x.Value.ElapsedMs);
    bool orderingCards;
    void OrderCollectionCards(GroupView group,CollectionRun job)
    {
        if(orderingCards)return;orderingCards=true;
        try{var tiles=rows.Where(r=>group.Members.Any(p=>p.Id==r.Profile.Id)).ToDictionary(r=>r.Profile.Id,r=>r.Tile);var order=CollectionProbePlan.DisplayOrder(group.Members,PassedTimes(job)).OrderByDescending(p=>data.PinnedProfileIds.Contains(p.Id)).ToArray();for(int i=0;i<order.Length;i++){var tile=tiles[order[i].Id];if(group.Paths.Children.IndexOf(tile)!=i){group.Paths.Children.Remove(tile);group.Paths.Children.Insert(i,tile);}}}finally{orderingCards=false;}
    }
    void StartCollectionTest(List<Profile>? profiles,int attempts,bool tcp)
    {
        CancelRouting28();
        var members=(profiles??SelectedGroup()).Where(p=>SupportPolicy.Allowed(p,data.IncludeExperimental)).ToList();if(members.Count==0)return;
        string key=CollectionKey(members[0]);
        if(currentCollection is{Running:true}){if(currentCollection.Key==key)CancelCollectionTest();return;}
        if(busy||closing||state.State is "connecting" or "disconnecting"||!tcp&&state.State=="connected")return;
        if(tcp&&!members.Any(CollectionProbePlan.IsDirect)){Notify(L.Text("QUICK_DNS_EXCLUDED"),Muted);return;}
        collectionTask=tcp?RunQuickPing29(members):RunCollection(members,Math.Clamp(attempts,1,3),false);
    }
    async Task RunCollection(List<Profile> members,int attempts,bool tcp)
    {
        var fresh=data.Measurements.Where(x=>x.Value.Verified&&UiPolicy.MeasurementFresh(x.Value.SampledAt,DateTimeOffset.UtcNow)).ToDictionary(x=>x.Key,x=>x.Value.ElapsedMs);
        var plan=new CollectionProbePlan(members,fresh);var job=new CollectionRun{Key=CollectionKey(members[0]),Tcp=tcp,Running=true,Total=members.Count};collectionRuns[job.Key]=currentCollection=job;
        if(!tcp){data.CollectionTestStarted[job.Key]=DateTimeOffset.UtcNow;liveMeasurements.ExceptWith(members.Select(p=>p.Id));}
        foreach(var p in plan.Direct)job.Pending[p.Id]="GROUP_WAIT_DIRECT";foreach(var p in plan.Dns)job.Pending[p.Id]="GROUP_WAIT_DNS";
        testStop=new();var token=testStop.Token;RefreshCards();
        try{
            string url=tcp?"":SubscriptionUrls.Validate(data.LatencyUrl).AbsoluteUri;
            await Run(async()=>{
                async Task Test(Profile p){
                    if(token.IsCancellationRequested)return;job.Current=p.Name;job.Pending[p.Id]="GROUP_TESTING";RefreshCards();
                    AgentReply last=new();int success=0,completedAttempts=0;long total=0;
                    for(int i=0;i<attempts&&!token.IsCancellationRequested;i++){
                        completedAttempts++;
                        try{
                            if(collectionProbeFixture!=null)last=await collectionProbeFixture(p,token);
                            else if(tcp){long? ms=await CollectionActions.Tcp(p,token);last=new(){ElapsedMs=ms??0,Error=ms.HasValue?"":"TCP_NOT_APPLICABLE"};}
                            else last=await ProbeCollectionProfile(p,url,token);
                            if(tcp?last.Error.Length==0:last.Verified){success++;total+=last.ElapsedMs;}
                        }catch(OperationCanceledException){last=new(){Error=token.IsCancellationRequested?"CANCELLED":"TEST_TIMEOUT"};}
                        catch(SafeError e){last=new(){Error=e.Code};}
                        // Once an attempt fails, this all-attempts-required stability
                        // result cannot pass; do not repeat the same long timeout.
                        if(tcp?last.Error.Length>0:!last.Verified)break;
                    }
                    if(token.IsCancellationRequested)return;
                    bool passed=success==attempts;last.Verified=!tcp&&passed;if(passed)last.ElapsedMs=total/attempts;
                    if(!tcp&&!passed&&success>0)last.Error="PROBE_PARTIAL";
                    job.Pending.Remove(p.Id);job.Results[p.Id]=last;job.Completed++;if(passed)job.Passed++;
                    if(!tcp){measurements[p.Id]=last;data.Measurements[p.Id]=new(){Verified=last.Verified,ProbeFallbackUsed=last.ProbeFallbackUsed,ElapsedMs=last.ElapsedMs,Error=last.Error,Stage=last.ProbeStage,Attempts=completedAttempts,Passes=success,SampledAt=DateTimeOffset.UtcNow};liveMeasurements.Add(p.Id);}
                    RefreshCards();
                }
                foreach(var p in plan.Direct){if(token.IsCancellationRequested)break;await Test(p);}
                if(plan.NeedsDns(token.IsCancellationRequested,job.Passed)){
                    job.Phase="GROUP_DNS";RefreshCards();foreach(var p in plan.Dns){if(token.IsCancellationRequested)break;await Test(p);}
                }else if(!token.IsCancellationRequested)foreach(var p in plan.Dns)job.Pending[p.Id]="GROUP_DNS_SKIPPED";
            });
            if(lastError.Length>0&&!token.IsCancellationRequested)job.Error=lastError;
        }catch(SafeError e){job.Error=e.Code;}
        finally{
            job.Cancelled=token.IsCancellationRequested;job.Running=false;job.Current="";
            foreach(var id in job.Pending.Where(x=>x.Value!="GROUP_DNS_SKIPPED").Select(x=>x.Key).ToArray())job.Pending[id]=job.Cancelled?"CANCELLED":job.Error.Length>0?job.Error:"GROUP_NOT_TESTED";
            testStop.Dispose();testStop=null;var order=CollectionProbePlan.DisplayOrder(members,PassedTimes(job));var ids=members.Select(p=>p.Id).ToHashSet();int next=0;data.Profiles=data.Profiles.Select(p=>ids.Contains(p.Id)?order[next++]:p).ToList();try{Save();}catch{job.Error="SAVE_FAILED";}job.FinishedAt30=DateTimeOffset.UtcNow;BuildCards();RefreshCards();
        }
    }
    async Task<AgentReply> ProbeCollectionProfile(Profile p,string url,CancellationToken token,bool allowFallbacks=true)
    {
        p=GlobalResolvers.ForConnection(p,data.GlobalResolverEnabled,data.GlobalResolverList).Effective;
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(RouteSelection28.TestBudget(p));
        var probe=testing=new AgentClient(probeOnly:true);using var abort=deadline.Token.Register(probe.Abort);
        try{
            await EnsureConnectionCertificates29(p,deadline.Token);
            await probe.Start(deadline.Token);var reply=await probe.Send(new(){Action="connect",Profile=p,SystemProxy=false,SlipCertificate=StoredSlipPin(p),SlipInterval=SlipInterval(p),SshTlsCa=StoredSshTlsCa(p),HostKey=StoredSshKey(p),ChainIdentities=ChainIdentityOptions25(p)});
            if(reply.State=="connected")return await probe.Send(new(){Action="probe",ProbeUrl=url,ProbeFallbacks=allowFallbacks&&data.ProbeFallbacks,ProbeTimeoutMs=(int)RouteSelection28.TestBudget(p).TotalMilliseconds});
            return reply;
        }catch(SafeError e){return new(){Error=deadline.IsCancellationRequested?token.IsCancellationRequested?"CANCELLED":"TEST_TIMEOUT":e.Code};}
        finally{probe.Abort();await probe.DisposeAsync();if(ReferenceEquals(testing,probe))testing=null;}
    }
    void RenderCollectionRow(RowView row)
    {
        if(!collectionRuns.TryGetValue(CollectionKey(row.Profile),out var job))return;
        if(job.Pending.TryGetValue(row.Profile.Id,out var phase)){row.Result.Visibility=Visibility.Visible;row.Result.Text=L.Text(phase);row.Result.Foreground=phase=="QUICK_DNS_EXCLUDED"?Muted:MessageInk28(phase);row.Timing.Foreground=Muted;row.Timing.Visibility=data.Measurements.GetValueOrDefault(row.Profile.Id)?.Verified==true?Visibility.Visible:Visibility.Collapsed;}
        else if(job.Tcp&&job.Results.TryGetValue(row.Profile.Id,out var reply)){row.Result.Visibility=row.Timing.Visibility=Visibility.Visible;row.Result.Text=reply.Error.Length==0?L.Text("TCP_REPLIED"):L.Text(reply.Error);row.Result.Foreground=row.Timing.Foreground=reply.Error.Length==0?Green:Red;row.Timing.Text=reply.Error.Length==0?reply.ElapsedMs.ToString(CultureInfo.InvariantCulture)+" ms":"—";}
    }
    void RenderCollectionProgress(GroupView group)
    {
        collectionRuns.TryGetValue(group.Key,out var job);bool running=job?.Running==true;
        bool stopping=running&&testStop?.IsCancellationRequested==true;group.Stop.Visibility=running?Visibility.Visible:Visibility.Collapsed;group.Stop.IsEnabled=running&&!stopping;group.Stop.Content=L.Text(stopping?"CANCELLING":"STOP_TEST");
        group.Test.Visibility=group.Connect.Visibility=group.More.Visibility=running?Visibility.Collapsed:Visibility.Visible;
        bool quickAllowed=group.Members.Any(p=>SupportPolicy.Allowed(p,data.IncludeExperimental)&&CollectionProbePlan.IsDirect(p));
        group.Test.IsEnabled=running||quickAllowed&&!busy&&state.State is not("connecting" or "disconnecting");group.Test.ToolTip=L.Text(running?"STOP_TEST":quickAllowed?"QUICK_PING":"QUICK_DNS_EXCLUDED");System.Windows.Automation.AutomationProperties.SetName(group.Test,L.Text(running?"STOP_TEST":"QUICK_PING"));
        group.Test.Content=new System.Windows.Shapes.Path{Data=Geometry.Parse(running?"M5,5 H19 V19 H5 Z":"M4,5 H20 V20 H4 Z M8,2 V8 M16,2 V8 M8,13 L11,16 L17,10"),Stroke=Tone("Ink"),StrokeThickness=1.7,Width=22,Height=22,Stretch=Stretch.Uniform};
        group.Progress.Visibility=running?Visibility.Visible:Visibility.Collapsed;
        if(job==null)return;OrderCollectionCards(group,job);group.Progress.Maximum=Math.Max(1,job.Total);group.Progress.Value=job.Completed;group.Summary.Visibility=Visibility.Visible;
        group.Summary.Foreground=job.Error.Length>0?MessageInk28(job.Error):Muted;
        group.Summary.Text=CollectionRunText30(job);
    }
}
