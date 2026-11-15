using Rovunet.Core;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    int CommitPublicBatch28(Uri uri,CompatibleImport batch,string name,string? retainedSource=null)
    {
        if(!PublicFeed.IsKnown(uri.AbsoluteUri)||!PublicFeed.IsPartial(batch)){
            PublicFeed.ValidateForApply(uri.AbsoluteUri,batch);
            CommitSubscription20(uri,batch.Profiles,name,retainedSource);return batch.Profiles.Count;
        }
        if(batch.Profiles.Count==0)throw new SafeError("NO_SUPPORTED_IMPORT");
        var preset=PublicFeed.Resolve(uri.AbsoluteUri)!;
        var sub=retainedSource!=null?data.Subscriptions.FirstOrDefault(s=>s.Id==retainedSource):data.Subscriptions.FirstOrDefault(s=>ManagedSource28(s)?.Key==preset.Key);
        string source=sub?.Id??Importer.Fingerprint(uri.AbsoluteUri),title=sub?.Name??(name.Trim().Length>0?name.Trim():preset.DisplayName);
        var items=data.Profiles.Where(p=>p.SourceId==source).Select(p=>p.ItemId.Length>0?p.ItemId:Importer.Fingerprint(p.Raw)).ToHashSet();
        var ids=data.Profiles.Select(p=>p.Id).ToHashSet();var additions=new List<Profile>();
        foreach(var profile in batch.Profiles){
            string item=profile.ItemId.Length>0?profile.ItemId:Importer.Fingerprint(profile.Raw);
            if(!profile.CanConnect||!items.Add(item))continue;
            profile.ItemId=item;profile.SourceId=source;profile.CollectionId="sub:"+source;profile.CollectionName=title;
            if(!ids.Add(profile.Id)){profile.Id=Importer.Fingerprint("public/"+source+"/"+item);if(!ids.Add(profile.Id))continue;}
            additions.Add(profile);
        }
        // An incomplete source is add-only: never delete, overwrite, rebind trust,
        // move a manual entry or mark the last complete refresh as fresh.
        var previousProfiles=data.Profiles;var previousSubs=data.Subscriptions;
        data.Profiles=[..previousProfiles,..additions];
        if(sub==null)data.Subscriptions=[..previousSubs,new(){Id=source,Name=title,Url=preset.Url,PublicSource=preset.Key}];
        try{Save();}catch{data.Profiles=previousProfiles;data.Subscriptions=previousSubs;throw;}
        BuildCards();return additions.Count;
    }
}
