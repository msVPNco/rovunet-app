using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rovunet.Core;
namespace Rovunet.Platform;
public static class ProtocolHandshakeClient30
{
    static readonly HashSet<string> Errors=["INVALID_MESSAGE","HANDSHAKE_TIMEOUT","HANDSHAKE_REJECTED","HANDSHAKE_UNCONFIRMED","HANDSHAKE_DNS_FAILED","HANDSHAKE_FAILED","HANDSHAKE_UNSUPPORTED","TLS_UNVERIFIED","CANCELLED"];
    public static async Task<QuickProbe30> Probe(HandshakePlan30 plan,CancellationToken stop)
    {
        stop.ThrowIfCancellationRequested();string path=Path.Combine(AppContext.BaseDirectory,"engines","rovunet-handshake.exe");
        using(var binary=File.OpenRead(path))if(!Convert.ToHexString(SHA256.HashData(binary)).Equals(EnginePins.Handshake,StringComparison.OrdinalIgnoreCase))throw new SafeError("ENGINE_HASH_MISMATCH");
        using var budget=CancellationTokenSource.CreateLinkedTokenSource(stop);budget.CancelAfter(TimeSpan.FromSeconds(12));
        using var job=new ProcessJob();using var process=new Process{StartInfo=new(path){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardInputEncoding=new UTF8Encoding(false),StandardOutputEncoding=new UTF8Encoding(false)}};
        try{
            if(!process.Start())throw new SafeError("HANDSHAKE_FAILED");job.Assign(process);
            using var cancel=budget.Token.Register(()=>{try{process.Kill(true);}catch{}});
            var errors=Drain(process.StandardError,budget.Token);var output=Read(process.StandardOutput,budget.Token);
            string input=plan.Configuration.ToJsonString();if(input.Length>65536)throw new SafeError("MESSAGE_TOO_LARGE");await process.StandardInput.WriteAsync(input.AsMemory(),budget.Token);process.StandardInput.Close();
            await process.WaitForExitAsync(budget.Token);string json=await output;await errors;stop.ThrowIfCancellationRequested();
            if(process.ExitCode!=0)throw new SafeError("HANDSHAKE_FAILED");return Parse(json,plan);
        }catch(OperationCanceledException)when(!stop.IsCancellationRequested){return new(false,0,"HANDSHAKE_TIMEOUT",plan.Mechanism);}
        catch when(stop.IsCancellationRequested){throw new OperationCanceledException(stop);}
        finally{try{if(!process.HasExited)process.Kill(true);}catch{}}
    }
    static async Task<string> Read(StreamReader reader,CancellationToken stop){var text=new StringBuilder();var buffer=new char[512];int n;while((n=await reader.ReadAsync(buffer.AsMemory(),stop))>0){if(text.Length+n>2048)throw new SafeError("INVALID_MESSAGE");text.Append(buffer,0,n);}return text.ToString();}
    static async Task Drain(StreamReader reader,CancellationToken stop){var buffer=new char[512];while(await reader.ReadAsync(buffer.AsMemory(),stop)>0){}}
    public static QuickProbe30 Parse(string json,HandshakePlan30 plan)
    {
        try{
            using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=3});var root=doc.RootElement;
            string[] keys=["version","authenticated","mechanism","elapsed_ms","error"];var fields=root.EnumerateObject().Select(p=>p.Name).ToArray();if(fields.Length!=keys.Length||fields.Distinct().Count()!=keys.Length||fields.Any(p=>!keys.Contains(p))||root.GetProperty("version").GetInt32()!=1)throw new SafeError("INVALID_MESSAGE");
            bool authenticated=root.GetProperty("authenticated").GetBoolean();string mechanism=root.GetProperty("mechanism").GetString()??"",error=root.GetProperty("error").GetString()??"";long ms=root.GetProperty("elapsed_ms").GetInt64();
            if(authenticated&&(mechanism!=plan.Mechanism||error.Length!=0||ms is <0 or >10000))throw new SafeError("INVALID_MESSAGE");
            if(!authenticated){if(!Errors.Contains(error))throw new SafeError("INVALID_MESSAGE");return new(false,0,error,plan.Mechanism);}
            return new(true,ms,"",mechanism);
        }catch(SafeError){throw;}catch{throw new SafeError("INVALID_MESSAGE");}
    }
}
