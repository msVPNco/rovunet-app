using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;
using Rovunet.Platform;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunQuickScope35(string report)
    {
        var checks=new Dictionary<string,bool>();int accepted=0,received=0,pointers=0,saves=0;string phase="start",folder=Path.GetDirectoryName(report)!;Directory.CreateDirectory(folder);var proxyBefore=new WinInetProxy().Read();
        using var lifetime=new CancellationTokenSource(TimeSpan.FromSeconds(100));using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;
        var server=Task.Run(async()=>{try{while(!lifetime.IsCancellationRequested){using var socket=await listener.AcceptTcpClientAsync(lifetime.Token);Interlocked.Increment(ref accepted);var bytes=new byte[32];int count=await socket.GetStream().ReadAsync(bytes,lifetime.Token);Interlocked.Add(ref received,count);}}catch(OperationCanceledException){}catch(SocketException){}});
        async Task<bool> Wait(string expression){for(int i=0;i<160;i++){if(htmlReady29&&await html29!.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(4))=="true")return true;await Task.Delay(25);}return false;}
        async Task Click(string selector){if(!await Wait("document.querySelector("+JsonSerializer.Serialize(selector)+")"))throw new InvalidOperationException("Missing quick test control");await html29!.CoreWebView2.ExecuteScriptAsync("document.querySelector("+JsonSerializer.Serialize(selector)+").scrollIntoView({block:'center'})");await Task.Delay(40);using var point=JsonDocument.Parse(await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const b=document.querySelector("+JsonSerializer.Serialize(selector)+"),r=b.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return{x,y,hit:b.contains(document.elementFromPoint(x,y)),disabled:b.disabled}})()"));var p=point.RootElement;if(!p.GetProperty("hit").GetBoolean()||p.GetProperty("disabled").GetBoolean())throw new InvalidOperationException("Quick test control unavailable");foreach(string type in new[]{"mousePressed","mouseReleased"})await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type,x=p.GetProperty("x").GetDouble(),y=p.GetProperty("y").GetDouble(),button="left",clickCount=1}));pointers++;}
        try{
            storeSaveFixture28=()=>saves++;
            var direct=Enumerable.Range(1,20).Select(i=>Importer.Single("socks5://127.0.0.1:"+port+"#Direct-"+i)).ToArray();
            var dns=new[]{"dnstt","dnstt_ssh","sayedns","sayedns_ssh","soar","soar_ssh","vaydns","vaydns_ssh","slipstream","slipstream_ssh"}.Select(kind=>Importer.Single(ManualProfiles.Dns(kind,kind,"t.example.invalid","127.0.0.1:5353","udp",new string('a',64),"127.0.0.1","22","fixture","fixture"))).ToArray();
            foreach(var p in direct.Concat(dns)){p.CollectionId="quickscope35";p.CollectionName="Quick scope";}
            data.Profiles=dns.Concat(direct).ToList();data.SelectedId=direct[0].Id;data.OpenCollectionId="quickscope35";data.CollectionExpansionRevision=1;L.Language=data.Language="en";Width=620;Build();await Idle18();
            checks["ten_dns_variants_remain_connectable_and_manually_available"]=dns.Length==10&&dns.All(p=>p.CanConnect&&data.Profiles.Contains(p));
            QuickPing29(data.Profiles.ToList());await collectionTask!.WaitAsync(TimeSpan.FromSeconds(8));await Task.Delay(90);var run=collectionRuns["quickscope35"];
            checks["quick_thirty_card_group_tests_only_twenty_direct"]=accepted==20&&received==0&&run.Total==20&&run.Completed==20&&run.Passed==20&&run.ExcludedDns==10;
            checks["dns_gets_no_fake_quick_result_or_ping_measurement"]=dns.All(p=>!run.Results.ContainsKey(p.Id)&&!data.PingMeasurements.ContainsKey(p.Id)&&run.Pending[p.Id]=="QUICK_DNS_EXCLUDED");
            checks["quick_keeps_accurate_measurements_separate"]=data.Measurements.Count==0&&testing==null&&client==null;
            checks["excluded_dns_explanation_is_muted"]=rows.Where(r=>dns.Contains(r.Profile)).All(r=>r.Result.Text==L.Text("QUICK_DNS_EXCLUDED")&&r.Result.Foreground==Muted);
            phase="loopback-cleanup";lifetime.Cancel();listener.Stop();await server.WaitAsync(TimeSpan.FromSeconds(2));phase="all-direct-fail";var failedClock=System.Diagnostics.Stopwatch.StartNew();QuickPing29(data.Profiles.ToList());await collectionTask!.WaitAsync(TimeSpan.FromSeconds(20));run=collectionRuns["quickscope35"];
            checks["all_direct_failures_never_start_dns_fallback"]=run.Passed==0&&run.Completed==20&&run.Results.Count==20&&run.Total==20&&dns.All(p=>!run.Results.ContainsKey(p.Id)&&!data.PingMeasurements.ContainsKey(p.Id));
            checks["twenty_closed_ports_finish_within_four_worker_budget"]=failedClock.Elapsed.TotalSeconds<20;
            var hy=Importer.Single("hy2://fixture@server.example.invalid:443#Stop-fixture");hy.CollectionId="quickscope35";hy.CollectionName="Quick scope";data.Profiles=data.Profiles.Where(p=>p!=direct[0]).Prepend(hy).ToList();Build();
            var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);handshakeFixture30=async(_,plan,stop)=>{ready.TrySetResult();await Task.Delay(Timeout.Infinite,stop);return new(false,0,"CANCELLED",plan.Mechanism);};
            phase="pointer-start-stop";await Click(".collection .test-button[data-state='ping']");await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));checks["single_pointer_click_starts_quick_test_with_stop_state"]=await Wait("document.querySelector('.collection .test-button[data-state=\"stop\"]')?.getAttribute('aria-busy')==='true'");
            await Click(".collection .test-button[data-state='stop']");await collectionTask!.WaitAsync(TimeSpan.FromSeconds(5));checks["stop_pointer_cancels_without_starting_dns_or_https"]=collectionRuns["quickscope35"].Cancelled&&!collectionRuns["quickscope35"].Running&&dns.All(p=>!data.PingMeasurements.ContainsKey(p.Id))&&data.Measurements.Count==0;
            foreach(var language in new[]{"en","fa","ru","zh-CN"})foreach(var theme in new[]{"light","dark","amoled"}){L.Language=data.Language=language;data.Theme=theme;ApplyTheme();Build();await Idle18();string text=CollectionRunText30(collectionRuns["quickscope35"]);checks[language+theme+"_scope_counts_and_localized_hint"]=text.Contains(string.Format(L.Text("QUICK_DIRECT_SCOPE"),20,10))&&L.Text("QUICK_DNS_EXCLUDED")!="QUICK_DNS_EXCLUDED"&&L.Text("ROUTING_DIRECT_ONLY_HINT")!="ROUTING_DIRECT_ONLY_HINT";}
            var chain=Importer.Single(ChainProfiles.Create("DNS chain",new[]{dns[0],direct[0]}));chain.CollectionId="quickscope35";data.Profiles.Add(chain);data.CollectionRoutingModes["quickscope35"]="failover";data.SelectedId=hy.Id;activeProfile=hy;connectionAttempt28=hy;SetState(new(){State="error",Error="CORE_EXITED"});routingLastCheck28=0;routingCursor28.Clear();
            int probes=0,switched=0;routingProbeFixture28=(_,_)=>{probes++;return Task.FromResult(new AgentReply{Verified=true,ElapsedMs=100});};routingConnectFixture28=p=>{switched++;data.SelectedId=p.Id;activeProfile=p;SetState(new(){State="connected"});return Task.CompletedTask;};
            data.Profiles=dns.Append(chain).Append(hy).ToList();await TickRouting28(true);checks["automatic_failover_excludes_dns_and_dns_chain"]=probes==0&&switched==0&&activeProfile==hy;
            var alternate=direct[1];data.Profiles.Add(alternate);routingLastCheck28=0;await TickRouting28(true);checks["automatic_failover_still_selects_valid_direct_candidate"]=probes==1&&switched==1&&activeProfile==alternate;
            CancelRouting28(true);SetState(new());activeProfile=connectionAttempt28=null;routingProbeFixture28=null;routingConnectFixture28=null;
            data.Profiles=dns.ToList();data.PingMeasurements.Clear();collectionRuns.Clear();foreach(var p in dns)p.CollectionId="dns-only35";data.OpenCollectionId="dns-only35";data.SelectedId=dns[0].Id;Build();await Idle18();
            checks["dns_only_card_has_disabled_quick_button"]=!groups.Single().Test.IsEnabled&&await Wait("document.querySelector('.collection .test-button')?.disabled===true");
            Task? previous=collectionTask;QuickPing29(dns.ToList());checks["programmatic_dns_only_quick_does_not_create_test"]=ReferenceEquals(previous,collectionTask)&&collectionRuns.Count==0&&data.PingMeasurements.Count==0;
            var menu=PathMenu(dns[0],MenuAction("CONNECT",()=>{}),MenuAction("EDIT",()=>{}),MenuAction("DELETE",()=>{}));checks["dns_menu_keeps_resolver_scan_and_disables_quick"]=menu.Items.OfType<MenuItem>().Any(i=>Equals(i.Tag,"FIND_DNS"))&&!menu.Items.OfType<MenuItem>().Single(i=>Equals(i.Tag,"QUICK_PING")).IsEnabled;
            checks["no_agent_or_proxy_side_effects"]=client==null&&testing==null&&new WinInetProxy().Read()==proxyBefore&&saves>0;
        }catch(Exception e){checks["exception"]=false;File.WriteAllText(report+".error",phase+"\n"+e);}
        finally{lifetime.Cancel();listener.Stop();try{await server;}catch{}durationTimer.Stop();routingTimer28.Stop();handshakeFixture30=null;storeSaveFixture28=null;routingProbeFixture28=null;routingConnectFixture28=null;CancelRouting28(true);}
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,actualPointerClicks=pointers,loopbackTcpOnly=true,externalHttps=false,syntheticRoutingAndHandshakeAdapters=true,ownerStoreTouched=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
