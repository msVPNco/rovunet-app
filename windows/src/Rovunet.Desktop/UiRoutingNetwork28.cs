using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunRoutingNetwork28(string report)
    {
        var checks=new Dictionary<string,bool>();var before=new WinInetProxy().Read();var start=DateTimeOffset.UtcNow;
        using var lifetime=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;
        var clients=new ConcurrentBag<Task>();
        var serving=Task.Run(async()=>{
            try{while(!lifetime.IsCancellationRequested){var socket=await listener.AcceptTcpClientAsync(lifetime.Token);clients.Add(Task.Run(async()=>{
                using(socket)try{
                    var stream=socket.GetStream();var greeting=new byte[2];await stream.ReadExactlyAsync(greeting,lifetime.Token);await stream.ReadExactlyAsync(new byte[greeting[1]],lifetime.Token);await stream.WriteAsync(new byte[]{5,0},lifetime.Token);
                    var request=new byte[4];await stream.ReadExactlyAsync(request,lifetime.Token);int address=request[3] switch{1=>4,4=>16,3=>-1,_=>throw new IOException()};if(address<0){var n=new byte[1];await stream.ReadExactlyAsync(n,lifetime.Token);address=n[0];}
                    await stream.ReadExactlyAsync(new byte[address+2],lifetime.Token);await stream.WriteAsync(new byte[]{5,0,0,1,127,0,0,1,0,1},lifetime.Token);
                    // Deliberately hold TLS. All upstream traffic terminates on
                    // this owned loopback socket; no trust bypass is involved.
                    await Task.Delay(Timeout.Infinite,lifetime.Token);
                }catch{ }
            }));}}catch(OperationCanceledException){}catch(SocketException){}
        });
        await using var probe=new AgentClient(true);
        try{
            await probe.Start(lifetime.Token);
            var connected=await probe.Send(new(){Action="connect",SystemProxy=false,Profile=Importer.Single("socks5://127.0.0.1:"+port+"#Bounded-test")});
            checks["real_probe_only_agent_started"]=connected.State=="connected";
            var watch=Stopwatch.StartNew();var timed=await probe.Send(new(){Action="probe",ProbeUrl="https://example.invalid/",ProbeFallbacks=true,ProbeTimeoutMs=1000});
            checks["whole_round_not_each_fallback_is_bounded"]=watch.Elapsed.TotalSeconds<5&&timed.Error=="PROBE_TIMEOUT"&&!timed.Verified;
            var status=await probe.Send(new(){Action="telemetry"});checks["timeout_keeps_agent_and_tunnel_alive"]=status.State=="connected";
            var written=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending=probe.Send(new(){Action="probe",ProbeUrl="https://example.invalid/",ProbeTimeoutMs=25000},written:()=>written.SetResult());await written.Task;await Task.Delay(100);await probe.CancelProbe();
            var cancelled=await pending.WaitAsync(TimeSpan.FromSeconds(5));checks["explicit_cancel_returns_without_waiting_25s"]=cancelled.Error=="CANCELLED";
            status=await probe.Send(new(){Action="telemetry"});checks["cancel_keeps_pipe_framing_and_tunnel_alive"]=status.State=="connected";
            string raw="vless://00000000-0000-4000-8000-000000000001@127.0.0.1:443?type=tcp&security=reality&encryption=none&flow=xtls-rprx-vision-udp443&pbk="+Uri.EscapeDataString(Convert.ToBase64String(Enumerable.Repeat((byte)1,32).ToArray()).TrimEnd('=').Replace('+','-').Replace('/','_'))+"&sni=example.com";
            var p=Importer.Single(raw);string config=CoreLinks.Runtime(p,19091,19092);
            using(var doc=JsonDocument.Parse(config))checks["udp443_flow_preserved_in_engine_config"]=config.Contains("xtls-rprx-vision-udp443",StringComparison.Ordinal);
            string file=Path.Combine(Path.GetDirectoryName(report)!,"synthetic-udp443.json");File.WriteAllText(file,config);
            using var engine=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"engines","xray.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,ArgumentList={"run","-test","-config",file}})!;
            var output=engine.StandardOutput.ReadToEndAsync();var error=engine.StandardError.ReadToEndAsync();await engine.WaitForExitAsync(lifetime.Token);await output;await error;
            checks["pinned_xray_accepts_udp443_syntax"]=engine.ExitCode==0;
            checks["system_proxy_unchanged"]=before==new WinInetProxy().Read();
        }catch(Exception e){checks["exception"]=false;File.WriteAllText(report+".error",e.ToString());}
        finally{probe.Abort();lifetime.Cancel();listener.Stop();await serving;await Task.WhenAll(clients);}
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,startUtc=start,endUtc=DateTimeOffset.UtcNow,pid=Environment.ProcessId,scope="Real probe-only Agent + pinned core + owned loopback SOCKS sink; no live remote peer, no system proxy/TUN changes"},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
