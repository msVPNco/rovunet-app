using Rovunet.Core;

namespace Rovunet.Desktop;

public sealed partial class MainWindow
{
    readonly Dictionary<string,SubscriptionUsage20> subscriptionUsage20=[];
    readonly HashSet<string> ownAccountSources20=[];
    readonly HashSet<string> updatingSubscriptions20=[];
    readonly System.Windows.Threading.DispatcherTimer usageRefresh20=new(){Interval=TimeSpan.FromMinutes(10)};
    readonly System.Windows.Threading.DispatcherTimer officialFreeRefresh=new(){Interval=TimeSpan.FromHours(12)};
    readonly CancellationTokenSource usageLifetime20=new();
    bool refreshingUsage20, refreshingOfficialFree;
    // The official catalog is an opt-in source. Unlike third-party public
    // feeds it has a separately pinned Ed25519 key and can auto-refresh.
    async Task RefreshOfficialFree()
    {
        if(refreshingOfficialFree||fixtureMode||closing||busy||state.State!="disconnected"||!RovunetFreeCatalog.Enabled)return;
        refreshingOfficialFree=true;
        try{
            foreach(var sub in data.Subscriptions.ToArray()){
                if(usageLifetime20.IsCancellationRequested)return;
                if(ManagedSource28(sub)?.Key!="rovunet")continue;
                await UpdateCollection20(sub.Id);
            }
        }finally{refreshingOfficialFree=false;}
    }
    async Task RefreshAllUsage20()
    {
        if(refreshingUsage20||fixtureMode||closing)return;refreshingUsage20=true;
        try{
            foreach(var sub in data.Subscriptions.ToArray()){
                if(usageLifetime20.IsCancellationRequested)return;if(ManagedSource28(sub)!=null||PublicFeed.IsRemoved(sub.Url))continue;
                try{
                    var uri=SubscriptionUrls.Validate(sub.Url);
                    if(SubscriptionUrls.Status(uri)!=null)await RefreshStatus(uri,sub,usageLifetime20.Token);
                    else{
                        var payload=await SubscriptionClient.Fetch(uri,state.State=="connected"?state.HttpPort:0,cancellation:usageLifetime20.Token);CaptureUsage20(sub.Id,payload);
                        if(subscriptionUsage20.TryGetValue(sub.Id,out var usage)){sub.Usage=usage;Save();}
                    }
                    RefreshCards();
                }catch{ /* Retain the labelled cached snapshot when the network is unavailable. */ }
            }
        }finally{refreshingUsage20=false;}
    }
    void CaptureUsage20(string source,SubscriptionClient.Payload payload)
    {
        var usage=SubscriptionUsage20.Parse(payload.UserInfo,payload.Body,payload.Date);
        if(usage==null){
            if(subscriptionUsage20.TryGetValue(source,out var cached)){cached.CachedOnly=true;if(!ownAccountSources20.Contains(source))statuses[source]=cached.Snapshot(source);}
            return;
        }
        subscriptionUsage20[source]=usage;
        if(!ownAccountSources20.Contains(source))statuses[source]=usage.Snapshot(source);
    }
    void CommitSubscription20(Uri uri,List<Profile> imported,string name,string? retainedSourceId=null)
    {
        if(imported.Count==0)throw new SafeError("NO_SUPPORTED_IMPORT");uri=SubscriptionUrls.Validate(PublicFeed.CanonicalUrl(uri.AbsoluteUri));var preset=PublicFeed.Resolve(uri.AbsoluteUri);
        string source=retainedSourceId??(preset!=null?data.Subscriptions.FirstOrDefault(s=>ManagedSource28(s)?.Key==preset.Key)?.Id:null)??Importer.Fingerprint(uri.AbsoluteUri);
        if(statuses.GetValueOrDefault(source)?.Error=="ACCOUNT_BINDING_CHANGED")throw new SafeError("ACCOUNT_BINDING_CHANGED");
        var old=data.Profiles.Where(p=>p.SourceId==source).GroupBy(p=>p.ItemId.Length>0?p.ItemId:Importer.Fingerprint(p.Raw)).ToDictionary(g=>g.Key,g=>g.First());
        string collectionName=name.Trim().Length>0?name.Trim():data.Subscriptions.FirstOrDefault(s=>s.Id==source)?.Name??PublicFeed.NameFor(uri.AbsoluteUri);
        var overrides=new Dictionary<string,string>(data.LocalOverrideSources);var names=new Dictionary<string,string>(data.LocalOverrideNames);
        var outsideIds=data.Profiles.Where(p=>p.SourceId!=source).Select(p=>p.Id).ToHashSet();
        foreach(var profile in imported){
            profile.SourceId=source;profile.CollectionId="sub:"+source;if(collectionName.Length>0)profile.CollectionName=collectionName;
            if(profile.ItemId.Length==0)profile.ItemId=Importer.Fingerprint(profile.Raw);
            if(outsideIds.Contains(profile.Id))profile.Id=Importer.Fingerprint("subscription/"+source+"/"+profile.ItemId);
            if(!old.TryGetValue(profile.ItemId,out var prior))continue;
            SubscriptionEndpoint.RequireSame(data.LocalOverrideSources.GetValueOrDefault(prior.Id,prior.Raw),profile);
            profile.Id=prior.Id;
            if(data.LocalOverrideSources.TryGetValue(prior.Id,out string? baseline)){
                string incoming=profile.Raw,incomingName=profile.Name;var local=ProfileEditing.MergeSubscription(baseline,prior,profile,data.LocalOverrideNames.GetValueOrDefault(prior.Id));
                profile.Raw=local.Raw;profile.Name=local.Name;profile.Protocol=local.Protocol;profile.BlockReason=local.BlockReason;overrides[prior.Id]=incoming;names[prior.Id]=incomingName;
            }
        }
        var profiles=data.Profiles.Where(p=>p.SourceId!=source||preset==null&&!SupportPolicy.Allowed(p,data.IncludeExperimental)).Concat(imported).GroupBy(p=>p.Id).Select(g=>g.Last()).ToList();
        var subscriptions=data.Subscriptions.Where(s=>s.Id!=source).ToList();var previousSub=data.Subscriptions.FirstOrDefault(s=>s.Id==source);
        var preparedStatus=PrepareStatusCommit21(source,previousSub);
        subscriptions.Add(new(){Id=source,Url=uri.AbsoluteUri,PublicSource=preset?.Key??previousSub?.PublicSource??"",Name=imported[0].CollectionName.Length>0?imported[0].CollectionName:L.Text("COLLECTION"),StatusBody=statusBodies.GetValueOrDefault(source,previousSub?.StatusBody??""),StatusSchemaVersion=statusSchemas21.GetValueOrDefault(source,previousSub?.StatusSchemaVersion??0),Status2Authority=statusAuthorities21.GetValueOrDefault(source)??previousSub?.Status2Authority,Usage=subscriptionUsage20.GetValueOrDefault(source)??previousSub?.Usage});
        if(preparedStatus is {} prepared){subscriptions[^1].Status2Authority=prepared.Authority;subscriptions[^1].StatusOrigin=prepared.Origin;}
        var previous=(data.Profiles,data.Subscriptions,data.LocalOverrideSources,data.LocalOverrideNames,data.SelectedId,data.Status2Authorities);var timestamp=data.SubscriptionUpdated.GetValueOrDefault(source);
        data.Profiles=profiles;data.Subscriptions=subscriptions;data.LocalOverrideSources=overrides;data.LocalOverrideNames=names;data.SubscriptionUpdated[source]=DateTimeOffset.UtcNow;
        if(preparedStatus is {} state2)data.Status2Authorities=new(data.Status2Authorities){[state2.Scope]=state2.Authority};
        if(data.SelectedId.Length>0&&!profiles.Any(p=>p.Id==data.SelectedId))data.SelectedId="";
        try{Save();}catch{(data.Profiles,data.Subscriptions,data.LocalOverrideSources,data.LocalOverrideNames,data.SelectedId,data.Status2Authorities)=previous;if(timestamp==default)data.SubscriptionUpdated.Remove(source);else data.SubscriptionUpdated[source]=timestamp;throw;}
        BuildCards();
    }
    // A signed and valid empty official catalog must retire expired members
    // without deleting the source itself or touching any private collection.
    // An unsigned, third-party or failed download never enters this path.
    void CommitEmptyOfficialFree20(Subscription sub)
    {
        if(ManagedSource28(sub)?.Key!="rovunet")throw new SafeError("INVALID_IMPORT");
        var oldProfiles=data.Profiles;
        var oldSelected=data.SelectedId;
        var oldUpdated=data.SubscriptionUpdated.GetValueOrDefault(sub.Id);
        data.Profiles=data.Profiles.Where(p=>p.SourceId!=sub.Id).ToList();
        if(oldSelected.Length>0&&!data.Profiles.Any(p=>p.Id==oldSelected))data.SelectedId="";
        data.SubscriptionUpdated[sub.Id]=DateTimeOffset.UtcNow;
        try{Save();}
        catch{
            data.Profiles=oldProfiles;data.SelectedId=oldSelected;
            if(oldUpdated==default)data.SubscriptionUpdated.Remove(sub.Id);
            else data.SubscriptionUpdated[sub.Id]=oldUpdated;
            throw;
        }
        BuildCards();
    }
    async Task UpdateCollection20(string source)
    {
        var sub=data.Subscriptions.FirstOrDefault(s=>s.Id==source);if(sub==null||!updatingSubscriptions20.Add(source))return;
        subscriptionOutcomes30.Remove(source);
        string validationSummary="";
        try{
            RefreshCards();Notify(L.Text("SUBSCRIPTION_UPDATING"));var uri=SubscriptionUrls.Validate(SubscriptionUrl28(sub));bool publicFeed=PublicFeed.IsKnown(uri.AbsoluteUri);int port=state.State=="connected"?state.HttpPort:0;var stop=usageLifetime20.Token;
            if(!publicFeed && subscriptionFetchFixture28==null)await RefreshStatus(uri,sub);
            if(!publicFeed)RequireV2StatusReady21(uri,source);
            var payload=publicFeed?await FetchPublic28(uri.AbsoluteUri,stop,port):subscriptionFetchFixture28!=null?await subscriptionFetchFixture28(uri,stop,port):await SubscriptionClient.Fetch(uri,port,cancellation:stop);
            if(!publicFeed)CaptureUsage20(source,payload);
            var batch=publicFeed?PublicFeed.Preview(payload.Body,data.IncludeExperimental):Importer.ReadCompatible(SubscriptionUsage20.ConfigBody(payload.Body),data.IncludeExperimental);
            validationSummary=ImportSummary(batch);
            stop.ThrowIfCancellationRequested();
            if(publicFeed && PublicFeed.Resolve(uri.AbsoluteUri)?.Key=="rovunet" && batch.Profiles.Count==0 && batch.Total==0)
            {
                CommitEmptyOfficialFree20(sub);
                subscriptionOutcomes30[source]=new("SUBSCRIPTION_UPDATED",DateTimeOffset.UtcNow);
                Notify(L.Text("SUBSCRIPTION_UPDATED"));
            }
            else if(publicFeed&&PublicFeed.IsPartial(batch)){
                int added=CommitPublicBatch28(uri,batch,sub.Name,sub.Id);
                subscriptionOutcomes30[source]=new("PUBLIC_PARTIAL_ADDED",DateTimeOffset.UtcNow,added,batch.Skipped.Values.Sum());
                Notify(string.Format(L.Text("PUBLIC_PARTIAL_ADDED"),added,batch.Skipped.Values.Sum())+" · "+ImportSummary(batch),Amber);
            }else{PublicFeed.ValidateForApply(uri.AbsoluteUri,batch);CommitSubscription20(uri,batch.Profiles,sub.Name,sub.Id);subscriptionOutcomes30[source]=new("SUBSCRIPTION_UPDATED",DateTimeOffset.UtcNow);Notify(L.Text("SUBSCRIPTION_UPDATED")+" · "+ImportSummary(batch));}
        }catch(Exception error){string code=error is SafeError safe?safe.Code:error is OperationCanceledException?"CANCELLED":"FETCH_FAILED";subscriptionOutcomes30[source]=new(code,DateTimeOffset.UtcNow);Notify(L.Text(code)+(code=="MAHSA_VALIDATION_FAILED"?" · "+validationSummary:""),MessageInk28(code));}
        finally{updatingSubscriptions20.Remove(source);RefreshCards();}
    }
}
