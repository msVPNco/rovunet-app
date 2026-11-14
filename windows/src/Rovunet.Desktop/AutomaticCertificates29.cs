using Rovunet.Core;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    CancellationTokenSource? certificateStop29;
    readonly SemaphoreSlim certificateGate31=new(1,1);
    Func<Uri,CancellationToken,Task<string>>? certificateFetchFixture29;
    bool CanAcquireSlipCertificate29(Profile profile)
    {
        if(!TransportPlan.UsesSlip(profile.Raw))return false;
        profile=CertificateProfile31(profile);
        var sub=data.Subscriptions.FirstOrDefault(s=>s.Id==profile.SourceId);
        if(sub==null||profile.ItemId.Length==0)return false;
        try{_=SlipTrust29.Companion(sub.Url);return true;}catch(SafeError){return false;}
    }
    string AutomaticSlipPin29(Profile p)
    {
        p=CertificateProfile31(p);
        var sub=data.Subscriptions.FirstOrDefault(s=>s.Id==p.SourceId);if(sub==null)return "";
        try{
            if(!data.SlipTrustPins.TryGetValue(SlipTrust29.Key(sub,p),out var pin))return "";
            var status=statuses.GetValueOrDefault(sub.Id)?.Status;
            if(status==null||pin.ServerId!=status.ServerId||pin.AccountId!=status.AccountId||pin.PeriodId!=status.PeriodId)return "";
            return pin.CertificatePem;
        }catch(SafeError){return "";}
    }
    async Task EnsureSlipCertificate29(Profile profile,CancellationToken cancellation)
    {
        if(!TransportPlan.UsesSlip(profile.Raw))return;
        await certificateGate31.WaitAsync(cancellation);
        try{await AcquireSlipCertificate31(CertificateProfile31(profile),cancellation);}
        finally{certificateGate31.Release();}
    }
    Profile CertificateProfile31(Profile profile)
    {
        if(profile.SourceId.Length>0)return profile;
        var matches=data.Profiles.Where(p=>p.Raw==profile.Raw&&p.SourceId.Length>0).ToArray();return matches.Length==1?matches[0]:profile;
    }
    async Task AcquireSlipCertificate31(Profile profile,CancellationToken cancellation)
    {
        string existing=StoredSlipPin(profile);if(existing.Length>0){SlipCertificate.ReadPublic(existing);return;}
        var sub=data.Subscriptions.FirstOrDefault(s=>s.Id==profile.SourceId)??throw new SafeError("CERT_AUTO_UNAVAILABLE");
        var uri=SlipTrust29.Companion(sub.Url);string sourceUrl=sub.Url,raw=profile.Raw,key=SlipTrust29.Key(sub,profile);
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellation);deadline.CancelAfter(TimeSpan.FromSeconds(12));
        if(fixtureMode&&certificateFetchFixture29==null)throw new SafeError("CERT_AUTO_UNAVAILABLE");
        if(certificateFetchFixture29==null)await RefreshStatus(SubscriptionUrls.Validate(sub.Url),sub,deadline.Token);
        var snapshot=statuses.GetValueOrDefault(sub.Id);var account=snapshot?.Status??throw new SafeError("CERT_AUTO_UNAVAILABLE");
        if(snapshot!.Error.Length>0)throw new SafeError(snapshot.Error);
        string body;
        try{body=certificateFetchFixture29!=null?await certificateFetchFixture29(uri,deadline.Token):(await SubscriptionClient.Fetch(uri,state.State=="connected"?state.HttpPort:0,262144,deadline.Token)).Body;}
        catch(SafeError error)when(error.Code is "STATUS_NOT_FOUND" or "FETCH_FAILED"){throw new SafeError(error.Code=="STATUS_NOT_FOUND"?"SLIP_TRUST_NOT_FOUND":"SLIP_TRUST_FETCH_FAILED");}
        var pin=SlipTrust29.Read(body,uri,profile,account);deadline.Token.ThrowIfCancellationRequested();
        if(sub.Url!=sourceUrl||profile.Raw!=raw||!ReferenceEquals(data.Subscriptions.FirstOrDefault(s=>s.Id==sub.Id),sub)||statuses.GetValueOrDefault(sub.Id)?.Status?.Identity!=account.Identity)throw new SafeError("CANCELLED");
        existing=StoredSlipPin(profile);if(existing.Length>0)return;
        bool had=data.SlipTrustPins.TryGetValue(key,out var prior);
        if(prior!=null&&(prior.CertificateSha256!=pin.CertificateSha256||prior.AccountId!=pin.AccountId||prior.ServerId!=pin.ServerId))throw new SafeError("CERT_PIN_CHANGED");
        data.SlipTrustPins[key]=pin;
        try{Save();}catch{if(had)data.SlipTrustPins[key]=prior!;else data.SlipTrustPins.Remove(key);throw;}
        Notify(L.Text("CERT_AUTO_SAVED"));
    }
    async Task EnsureConnectionCertificates29(Profile profile,CancellationToken cancellation)
    {
        if(ChainProfiles.IsChain(profile.Raw)){
            foreach(var layer in ChainProfiles.Read(profile.Raw).Layers){
                var matches=data.Profiles.Where(p=>p.Raw==layer.Raw&&p.SourceId.Length>0).ToArray();
                await EnsureConnectionCertificates29(matches.Length==1?matches[0]:layer,cancellation);
            }
        }else await EnsureSlipCertificate29(profile,cancellation);
    }
    async Task PrepareAutomaticCertificates29(Profile profile)
    {
        using var stop=new CancellationTokenSource();certificateStop29=stop;
        try{await EnsureConnectionCertificates29(profile,stop.Token);}finally{if(ReferenceEquals(certificateStop29,stop))certificateStop29=null;}
    }
}
