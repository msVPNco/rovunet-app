using System.IO;
using System.Text.Json;
using System.Windows;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunHandshake30(string report)
    {
        var checks=new Dictionary<string,bool>();string folder=Path.GetDirectoryName(report)!,store=Path.Combine(folder,"private-fixture");Directory.CreateDirectory(folder);var proxy=new WinInetProxy().Read();int calls=0,maxActive=0,active=0;
        try{
            storeSaveFixture28=()=>PrivateStore.SaveIn(store,"profiles.dpapi",data);
            string key=Convert.ToBase64String(Enumerable.Repeat((byte)1,32).ToArray()),peer=Convert.ToBase64String(Enumerable.Repeat((byte)2,32).ToArray());
            var hy=Importer.Single("hy2://fixture@server.example.invalid:443#HY2");var tuic=Importer.Single("tuic://00000000-0000-4000-8000-000000000001:fixture@server.example.invalid:443?alpn=h3#TUIC");
            string ini=$"[Interface]\nPrivateKey={key}\nAddress=10.0.0.2/32\n[Peer]\nPublicKey={peer}\nEndpoint=127.0.0.1:51820\nAllowedIPs=0.0.0.0/0";
            var wg=Importer.Single(ini);var awg=Importer.Single(ini.Replace("[Peer]","Jc=0\n[Peer]"));data.Profiles=[hy,tuic,wg,awg];foreach(var p in data.Profiles){p.CollectionId="handshake30";p.CollectionName="UDP fixture";}data.OpenCollectionId="handshake30";data.SelectedId=hy.Id;data.CollectionExpansionRevision=1;
            foreach(var p in data.Profiles)data.Measurements[p.Id]=new(){Verified=true,ElapsedMs=77,SampledAt=DateTimeOffset.UtcNow.AddHours(-1)};string https=JsonSerializer.Serialize(data.Measurements);
            handshakeFixture30=async(p,plan,stop)=>{calls++;active++;maxActive=Math.Max(maxActive,active);try{await Task.Delay(45,stop);return p==awg?new(false,0,"HANDSHAKE_REJECTED",plan.Mechanism):new(true,p==hy?999:p==tuic?1000:1001,"",plan.Mechanism);}finally{active--;}};
            L.Language=data.Language="en";Build();QuickPing29(data.Profiles.ToList());await collectionTask!.WaitAsync(TimeSpan.FromSeconds(8));
            checks["quick_dispatches_all_four_udp_protocols_with_two_handshake_workers"]=calls==4&&maxActive==2&&data.PingMeasurements.Count==4;
            checks["authenticated_quick_results_do_not_overwrite_https"]=https==JsonSerializer.Serialize(data.Measurements)&&collectionRuns["handshake30"].Results.Values.All(r=>!r.Verified);
            var rowHy=rows.Single(r=>r.Profile==hy);var rowTuic=rows.Single(r=>r.Profile==tuic);var rowWg=rows.Single(r=>r.Profile==wg);var rowAwg=rows.Single(r=>r.Profile==awg);
            checks["999_and_exact_1000_green_1001_amber_rejection_red"]=rowHy.Timing.Foreground==Green&&rowTuic.Timing.Foreground==Green&&rowWg.Timing.Foreground==Amber&&rowAwg.Result.Foreground==Red;
            checks["tuic_has_relay_proof_label_and_slow_has_explicit_label"]=rowTuic.Result.Text==L.Text("HANDSHAKE_RELAY_OK")&&rowWg.Result.Text.Contains(L.Text("PING_SLOW"));
            var loaded=PrivateStore.LoadFrom<UserData>(store,"profiles.dpapi")!;checks["mechanism_and_separate_https_survive_dpapi_reload"]=loaded.PingMeasurements[tuic.Id].Mechanism=="tuic-authenticated-relay"&&loaded.Measurements[tuic.Id].Verified;
            livePings29.Clear();RefreshCards();checks["historical_authenticated_ping_is_gray"]=rowHy.Timing.Foreground==Muted&&rowTuic.Timing.Foreground==Muted;
            int previousCalls=calls;SetState(new(){State="connected"});activeProfile=wg;string cached=JsonSerializer.Serialize(data.PingMeasurements);QuickPing29([wg]);await collectionTask!;
            checks["same_active_wireguard_identity_is_skipped_without_roaming"]=calls==previousCalls&&cached==JsonSerializer.Serialize(data.PingMeasurements)&&collectionRuns["handshake30"].Pending[wg.Id]=="HANDSHAKE_ACTIVE_CONFLICT"&&state.State=="connected";
            SetState(new());activeProfile=null;
            var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource<QuickProbe30>(TaskCreationOptions.RunContinuationsAsynchronously);
            handshakeFixture30=(_,_,_)=>{ready.TrySetResult();return release.Task;};var prior=data.PingMeasurements[hy.Id];string raw=hy.Raw;QuickPing29([hy]);await ready.Task;hy.Raw=raw+"-edited";release.SetResult(new(true,1,"","hysteria2-auth"));await collectionTask!;
            checks["late_result_cannot_green_an_edited_profile"]=ReferenceEquals(prior,data.PingMeasurements[hy.Id])&&collectionRuns["handshake30"].Pending[hy.Id]=="RESULT_STALE"&&!HasCurrentPing29(hy,out _);hy.Raw=raw;
            ready=new(TaskCreationOptions.RunContinuationsAsynchronously);handshakeFixture30=async(_,plan,stop)=>{ready.TrySetResult();await Task.Delay(Timeout.Infinite,stop);return new(false,0,"CANCELLED",plan.Mechanism);};cached=JsonSerializer.Serialize(data.PingMeasurements);QuickPing29([hy]);await ready.Task;CancelCollectionTest();await collectionTask!.WaitAsync(TimeSpan.FromSeconds(3));
            checks["stop_cancels_pending_handshake_and_keeps_prior_results"]=cached==JsonSerializer.Serialize(data.PingMeasurements)&&collectionRuns["handshake30"].Cancelled&&!collectionRuns["handshake30"].Running;
            handshakeFixture30=(_,plan,_)=>Task.FromResult(new QuickProbe30(true,20,"",plan.Mechanism));storeSaveFixture28=()=>throw new IOException("synthetic failure");QuickPing29([hy]);await collectionTask!;
            checks["save_failure_restores_previous_measurement_and_freshness"]=cached==JsonSerializer.Serialize(data.PingMeasurements)&&collectionRuns["handshake30"].Error=="SAVE_FAILED";
            checks["save_failure_cannot_leave_a_green_transient_result"]=rows.Single(r=>r.Profile==hy).Result.Text==L.Text("SAVE_FAILED")&&rows.Single(r=>r.Profile==hy).Result.Foreground==Red&&collectionRuns["handshake30"].Results.Count==0;
            checks["quick_has_no_agent_or_network_settings_side_effects"]=client==null&&testing==null&&new WinInetProxy().Read()==proxy&&https==JsonSerializer.Serialize(data.Measurements);
            checks["unconfirmed_is_amber_and_active_conflict_is_muted"]=MessageInk28("HANDSHAKE_UNCONFIRMED")==Amber&&MessageInk28("HANDSHAKE_ACTIVE_CONFLICT")==Muted;
        }catch(Exception error){checks["exception"]=false;File.WriteAllText(report+".error",error.ToString());}
        finally{durationTimer.Stop();routingTimer28.Stop();handshakeFixture30=null;storeSaveFixture28=null;}
        bool passed=checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,syntheticProbeAdapter=true,realCryptography="See native handshake suite and C# process interoperability receipt",ownerStoreTouched=false,liveRemoteNetwork=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
