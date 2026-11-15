using System.Windows.Media;
using Rovunet.Core;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    sealed record SubscriptionOutcome30(string Code,DateTimeOffset At,int Added=0,int Skipped=0);
    sealed record CardWork30(string Text,bool Running,Brush Ink);
    readonly Dictionary<string,SubscriptionOutcome30> subscriptionOutcomes30=[];
    string CollectionRunText30(CollectionRun run)
    {
        bool stopping=run.Running&&testStop?.IsCancellationRequested==true;
        string text=L.Text(run.Tcp?"QUICK_PING":"DETAILED_HTTPS")+" · "+(run.Running?L.Text(stopping?"CANCELLING":run.Phase)+" · "+run.Completed+"/"+run.Total+" · "+run.Current:L.Text(run.Cancelled?"CANCELLED":"GROUP_DONE")+" · "+string.Format(L.Text("GROUP_COUNTS"),run.Passed,run.Completed,run.Total)+(run.Error.Length>0?" · "+L.Text(run.Error):""));
        if(run.Tcp&&run.ExcludedDns>0)text+=" · "+string.Format(L.Text("QUICK_DIRECT_SCOPE"),run.Total,run.ExcludedDns);
        int unknown=run.Results.Values.Count(r=>!r.Verified&&(run.Tcp?r.Error is "TCP_NOT_APPLICABLE" or "HANDSHAKE_UNCONFIRMED":HttpsProbePolicy.Unconfirmed(r.Error)));return !run.Running&&unknown>0?text+" · "+string.Format(L.Text("GROUP_UNCONFIRMED_COUNT"),unknown):text;
    }
    CardWork30? CardActivity30(GroupView group)
    {
        string source=group.Members[0].SourceId;var messages=new List<string>();
        if(updatingSubscriptions20.Contains(source))messages.Add(L.Text("SUBSCRIPTION_UPDATING"));
        collectionRuns.TryGetValue(group.Key,out var run);
        if(run?.Running==true)messages.Add(CollectionRunText30(run));
        if(state.State is "connecting" or "disconnecting"&&group.Members.Any(p=>p.Id==(activeProfile??connectionAttempt28)?.Id))messages.Add(L.Text(state.State.ToUpperInvariant()));
        if(messages.Count>0)return new(string.Join(" · ",messages),true,Accent);
        if(subscriptionOutcomes30.TryGetValue(source,out var outcome)&&(run==null||outcome.At>=run.FinishedAt30)){
            string text=outcome.Code=="PUBLIC_PARTIAL_ADDED"?string.Format(L.Text(outcome.Code),outcome.Added,outcome.Skipped):L.Text(outcome.Code);
            return new(text,false,outcome.Code=="SUBSCRIPTION_UPDATED"?Green:outcome.Code=="PUBLIC_PARTIAL_ADDED"?Amber:MessageInk28(outcome.Code));
        }
        if(run!=null&&run.FinishedAt30!=default){Brush ink=run.Cancelled?Muted:run.Error.Length>0?MessageInk28(run.Error):run.Passed>0?Green:run.Results.Values.Any(r=>r.Error is "HANDSHAKE_UNCONFIRMED" or "TCP_NOT_APPLICABLE"||!run.Tcp&&HttpsProbePolicy.Unconfirmed(r.Error))?Amber:run.Completed>0?Red:Muted;return new(CollectionRunText30(run),false,ink);}
        return null;
    }
}
