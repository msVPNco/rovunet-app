using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunPingTrust29(string report)
    {
        var checks=new Dictionary<string,bool>();string folder=Path.GetDirectoryName(report)!;Directory.CreateDirectory(folder);string store=Path.Combine(folder,"private-fixture");var started=DateTimeOffset.UtcNow;var proxyBefore=new WinInetProxy().Read();
        using var lifetime=new CancellationTokenSource(TimeSpan.FromSeconds(45));using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port,received=0,accepted=0;var workers=new List<Task>();
        var server=Task.Run(async()=>{try{while(!lifetime.IsCancellationRequested){var socket=await listener.AcceptTcpClientAsync(lifetime.Token);Interlocked.Increment(ref accepted);workers.Add(Task.Run(async()=>{using(socket){try{var buffer=new byte[4096];int count;while((count=await socket.GetStream().ReadAsync(buffer,lifetime.Token))>0)Interlocked.Add(ref received,count);}catch(OperationCanceledException){}}}));}}catch(OperationCanceledException){}catch(SocketException){}});
        try{
            storeSaveFixture28=()=>PrivateStore.SaveIn(store,"profiles.dpapi",data);
            data.Profiles=Enumerable.Range(0,6).Select(i=>{var p=Importer.Single("socks5://127.0.0.1:"+port+"#Ping-"+i);p.CollectionId="ping29";p.CollectionName="Ping";return p;}).ToList();data.OpenCollectionId="ping29";data.CollectionExpansionRevision=1;data.SelectedId=data.Profiles[0].Id;
            var first=data.Profiles[0];data.Measurements[first.Id]=new(){Verified=true,ElapsedMs=800,SampledAt=DateTimeOffset.UtcNow.AddHours(-1)};string httpsBefore=JsonSerializer.Serialize(data.Measurements);Build();activeProfile=first;SetState(new(){State="connected"});
            groups.Single().Test.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await collectionTask!.WaitAsync(TimeSpan.FromSeconds(8));await Task.Delay(80);
            checks["card_button_runs_actual_tcp_ping_without_http_or_tls"]=accepted==6&&received==0&&collectionRuns["ping29"].Tcp&&collectionRuns["ping29"].Passed==6;
            checks["quick_ping_never_creates_agent_or_alters_active_connection"]=testing==null&&client==null&&state.State=="connected"&&activeProfile==first;
            checks["tcp_ping_does_not_overwrite_https_verification"]=httpsBefore==JsonSerializer.Serialize(data.Measurements);
            checks["fresh_ping_green_and_explicitly_tcp"]=rows.All(r=>r.Result.Text==L.Text("PING_REPLIED")&&r.Result.Foreground==Green&&r.Timing.Foreground==Green);
            var loaded=PrivateStore.LoadFrom<UserData>(store,"profiles.dpapi")!;checks["separate_ping_results_survive_dpapi_reload"]=loaded.PingMeasurements.Count==6&&loaded.Measurements[first.Id].ElapsedMs==800;
            string raw=first.Raw;first.Raw=raw+"-edited";checks["edited_profile_does_not_inherit_old_ping"]=!HasCurrentPing29(first,out _);first.Raw=raw;
            int probes=0;networkFixtureMode=true;SetState(new());activeProfile=null;collectionProbeFixture=(_,_)=>{probes++;return Task.FromResult(new AgentReply{Verified=true,ElapsedMs=40});};CollectionTestDialog([first]);await collectionTask!;
            checks["accurate_test_keeps_https_probe_and_separate_ping_history"]=probes==1&&!collectionRuns["ping29"].Tcp&&data.Measurements[first.Id].Verified&&data.PingMeasurements.Count==6&&!PreferPing29(first,out _);networkFixtureMode=false;collectionProbeFixture=null;
            var pathMenu=PathMenu(first,MenuAction("CONNECT",()=>{}),MenuAction("EDIT",()=>{}),MenuAction("DELETE",()=>{}));checks["card_menu_has_distinct_ping_and_https"]=new[]{"QUICK_PING","DETAILED_HTTPS"}.All(key=>pathMenu.Items.OfType<System.Windows.Controls.MenuItem>().Count(item=>Equals(item.Tag,key))==1);
            foreach(var (uri,hasTcp) in new[]{("trojan://fixture@server.example.invalid:443?security=tls",true),("anytls://fixture@server.example.invalid:443",true),("mierus://fixture:fixture@server.example.invalid?profile=Fixture&port=443&protocol=TCP",true),("mierus://fixture:fixture@server.example.invalid?profile=Fixture&port=443&protocol=UDP",false),("hy2://fixture@server.example.invalid:443",false)})checks["tcp_target_"+Importer.Single(uri).Protocol+hasTcp]=CollectionActions.TcpTarget(Importer.Single(uri)).HasValue==hasTcp;
            using var sampleStream=typeof(MainWindow).Assembly.GetManifestResourceStream("Rovunet.SlipTrust29.Sample")!;using var reader=new StreamReader(sampleStream);string sample=reader.ReadToEnd();var root=JsonNode.Parse(sample)!.AsObject();
            var slip=Importer.Single(ManualProfiles.Dns("slipstream","Slip fixture","slip.example.test","127.0.0.1:53","udp","","","","",""));slip.SourceId="trust29";slip.ItemId="slipstream-socks";data.Profiles.Add(slip);
            var sub=new Subscription{Id=slip.SourceId,Url="https://panel.example.test/sub/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.windows.json",Name="Trust fixture"};data.Subscriptions=[sub];
            checks["routing_can_acquire_subscribed_slip_certificate"]=CanAcquireSlipCertificate29(slip);
            string sourceBefore=sub.Url;sub.Url="https://panel.example.test/unrelated.json";checks["routing_does_not_guess_unknown_certificate_endpoint"]=!CanAcquireSlipCertificate29(slip);sub.Url=sourceBefore;
            AccountStatus Account(string period)=>new(){ServerId=root["server_id"]!.ToString(),AccountId=root["account_id"]!.ToString(),PeriodId=period};string period=root["period_id"]!.ToString();statuses[sub.Id]=new(){Status=Account(period)};
            int fetches=0;certificateFetchFixture29=(uri,_)=>{fetches++;checks["trust_companion_uses_same_origin_and_token"]=uri.AbsoluteUri=="https://panel.example.test/sub/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.trust.json";return Task.FromResult(sample);};
            await EnsureSlipCertificate29(slip,CancellationToken.None);await EnsureSlipCertificate29(slip,CancellationToken.None);
            checks["first_certificate_saved_without_prompt_and_repeat_is_idempotent"]=fetches==1&&data.SlipTrustPins.Count==1&&StoredSlipPin(slip).Length>0&&NavigationDepth==0;
            loaded=PrivateStore.LoadFrom<UserData>(store,"profiles.dpapi")!;checks["certificate_and_account_survive_dpapi_reload"]=loaded.SlipTrustPins.Single().Value.CertificatePem==StoredSlipPin(slip)&&loaded.Subscriptions.Single().Id==sub.Id;
            var detached=Importer.Single(slip.Raw);await EnsureSlipCertificate29(detached,CancellationToken.None);checks["unique_subscribed_chain_layer_reuses_its_bound_certificate"]=CanAcquireSlipCertificate29(detached)&&StoredSlipPin(detached)==StoredSlipPin(slip)&&fetches==1;
            var duplicate=Importer.Single(slip.Raw);duplicate.SourceId="different-source";data.Profiles.Add(duplicate);checks["ambiguous_detached_layer_does_not_guess_a_source"]=!CanAcquireSlipCertificate29(detached)&&StoredSlipPin(detached).Length==0;data.Profiles.Remove(duplicate);
            var cachedPin=data.SlipTrustPins.Single();data.SlipTrustPins.Clear();fetches=0;certificateFetchFixture29=async(_,token)=>{fetches++;await Task.Delay(100,token);return sample;};
            await Task.WhenAll(Enumerable.Range(0,5).Select(_=>EnsureSlipCertificate29(slip,CancellationToken.None)));checks["concurrent_scanner_candidates_fetch_and_save_certificate_once"]=fetches==1&&data.SlipTrustPins.Count==1;
            data.SlipTrustPins.Clear();certificateFetchFixture29=(_,_)=>throw new SafeError("FETCH_FAILED");bool certificateBeforeAgent=false;
            try{await VerifyCandidate(slip,slip,"https://fixture.example.invalid/",CancellationToken.None,()=>false,_=>{});}catch(SafeError e){certificateBeforeAgent=e.Code=="SLIP_TRUST_FETCH_FAILED";}
            checks["scanner_e2e_acquires_certificate_before_starting_agent_without_picker"]=certificateBeforeAgent&&client==null&&testing==null;
            string beforeTrustFailure=JsonSerializer.Serialize(data);int failureSaves=0;var priorSaver=storeSaveFixture28;storeSaveFixture28=()=>failureSaves++;certificateFetchFixture29=(_,_)=>throw new SafeError("STATUS_NOT_FOUND");bool descriptive404=false;
            try{await EnsureSlipCertificate29(slip,CancellationToken.None);}catch(SafeError e){descriptive404=e.Code=="SLIP_TRUST_NOT_FOUND";}
            checks["missing_trust_has_certificate_specific_error_without_data_or_save"]=descriptive404&&failureSaves==0&&JsonSerializer.Serialize(data)==beforeTrustFailure&&data.SlipTrustPins.Count==0;storeSaveFixture28=priorSaver;
            string priorLanguage=L.Language;foreach(string language in new[]{"en","fa","ru","zh-CN"}){L.Language=language;checks[language+"_certificate_fetch_errors_are_translated"]=new[]{"SLIP_TRUST_NOT_FOUND","SLIP_TRUST_FETCH_FAILED"}.All(key=>L.Text(key)!=key);}L.Language=priorLanguage;
            data.SlipTrustPins[cachedPin.Key]=cachedPin.Value;certificateFetchFixture29=(_,_)=>Task.FromResult(sample);
            data.SlipTrustPins.Clear();var acquisitionReady=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);certificateFetchFixture29=async(_,token)=>{acquisitionReady.TrySetResult();await Task.Delay(Timeout.Infinite,token);return sample;};
            data.SelectedId=slip.Id;data.DnsPoolEnabled=true;data.DnsPoolList="127.0.0.1:53";networkFixtureMode=true;BuildCards();var connecting=Run(Connect);await acquisitionReady.Task.WaitAsync(TimeSpan.FromSeconds(3));
            checks["connect_prepares_certificate_before_dns_pool_and_shows_cancellable_connecting"]=state.State=="connecting"&&connect.IsEnabled&&client==null&&poolStop==null;
            RequestDisconnect();await connecting.WaitAsync(TimeSpan.FromSeconds(3));checks["cancel_during_certificate_acquisition_restores_disconnected_without_agent"]=state.State=="disconnected"&&lastError.Length==0&&!busy&&client==null&&data.SlipTrustPins.Count==0;
            data.DnsPoolEnabled=false;networkFixtureMode=false;data.SlipTrustPins[cachedPin.Key]=cachedPin.Value;certificateFetchFixture29=(_,_)=>Task.FromResult(sample);
            var expected=Account(period);var endpoint=SlipTrust29.Companion(sub.Url);var good=SlipTrust29.Read(sample,endpoint,slip,expected);
            foreach(string mutation in new[]{"hash","domain","account","period","private","duplicate"}){
                var bad=JsonNode.Parse(sample)!;var item=bad["items"]![0]!;
                if(mutation=="hash")item["certificate_sha256"]=new string('0',64);if(mutation=="domain")item["domain"]="other.example.test";if(mutation=="account")bad["account_id"]=new string('0',32);if(mutation=="period")bad["period_id"]=new string('0',32);if(mutation=="private")item["certificate_pem"]=item["certificate_pem"]!.ToString()+"\n-----BEGIN PRIVATE KEY-----\ninvalid";if(mutation=="duplicate")bad["items"]!.AsArray().Add(item.DeepClone());
                bool rejected=false;try{SlipTrust29.Read(bad.ToJsonString(),endpoint,slip,expected);}catch(SafeError){rejected=true;}checks["trust_rejects_"+mutation]=rejected;
            }
            foreach(string source in new[]{"https://panel.example.test/sub/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.json","https://panel.example.test/v2/sub/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.windows.json"})checks["trust_url_"+source.Contains("/v2/")]=SlipTrust29.Companion(source).AbsolutePath.EndsWith("/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.trust.json",StringComparison.Ordinal);
            var oldPin=data.SlipTrustPins.Single().Value;string key=data.SlipTrustPins.Single().Key;data.SlipTrustPins.Clear();storeSaveFixture28=()=>throw new IOException("synthetic save failure");bool rolled=false;try{await EnsureSlipCertificate29(slip,CancellationToken.None);}catch(IOException){rolled=true;}checks["automatic_pin_save_failure_rolls_back"]=rolled&&data.SlipTrustPins.Count==0;storeSaveFixture28=()=>PrivateStore.SaveIn(store,"profiles.dpapi",data);data.SlipTrustPins[key]=oldPin;
            string nextPeriod=new string('8',32);statuses[sub.Id]=new(){Status=Account(nextPeriod)};using var ec=ECDsa.Create();var request=new CertificateRequest("CN=slip.example.test",ec,HashAlgorithmName.SHA256);using var replacement=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddYears(1));var replacementBody=JsonNode.Parse(sample)!;replacementBody["period_id"]=nextPeriod;replacementBody["items"]![0]!["certificate_pem"]=replacement.ExportCertificatePem();replacementBody["items"]![0]!["certificate_sha256"]=Convert.ToHexStringLower(SHA256.HashData(replacement.RawData));replacementBody["items"]![0]!["not_after"]=replacement.NotAfter.ToUniversalTime().ToString("O");certificateFetchFixture29=(_,_)=>Task.FromResult(replacementBody.ToJsonString());bool mismatch=false;try{await EnsureSlipCertificate29(slip,CancellationToken.None);}catch(SafeError e){mismatch=e.Code=="CERT_PIN_CHANGED";}checks["different_previous_pin_is_preserved"]=mismatch&&ReferenceEquals(data.SlipTrustPins[key],oldPin);
            checks["system_proxy_unchanged"]=proxyBefore==new WinInetProxy().Read();
        }catch(Exception error){checks["exception"]=false;File.WriteAllText(report+".error",error.ToString());}
        finally{durationTimer.Stop();networkFixtureMode=false;collectionProbeFixture=null;certificateFetchFixture29=null;storeSaveFixture28=null;lifetime.Cancel();listener.Stop();await server;await Task.WhenAll(workers);}
        bool passed=checks.Count>0&&checks.Values.All(value=>value);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,realLoopbackTcp=true,liveRemoteNetwork=false,ownerStoreTouched=false,receivedApplicationBytes=received,startUtc=started,endUtc=DateTimeOffset.UtcNow},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
