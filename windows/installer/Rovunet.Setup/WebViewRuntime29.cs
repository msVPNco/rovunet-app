using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;
namespace Rovunet.Setup;

internal static class WebViewRuntime29
{
    const string ExpectedSha256="cf2f2445fef0c5c14e372045f84998771eeb8bb01d59be4afd00692fa160082e";
    internal static bool Installed()
    {
        foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})
        foreach(var view in new[]{RegistryView.Registry32,RegistryView.Registry64}){
            using var root=RegistryKey.OpenBaseKey(hive,view);
            using var key=root.OpenSubKey(@"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}");
            if(Version.TryParse(key?.GetValue("pv") as string,out var version)&&version.Major>=120)return true;
        }
        return false;
    }
    internal static async Task Ensure(Action installing)
    {
        await EnsureCore(Installed,async()=>{
            installing();string folder=Path.Combine(Path.GetTempPath(),"Rovunet-WebView2-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);string path=Path.Combine(folder,"MicrosoftEdgeWebview2Setup.exe");
            try{
                using var source=Assembly.GetExecutingAssembly().GetManifestResourceStream("WebViewBootstrapper29")??throw new InstallProblem("runtimeFailed");
                // Keep the verified file open without write/delete sharing until the child exits.
                using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){await source.CopyToAsync(output);await output.FlushAsync();}
                using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
                    if(Convert.ToHexString(await SHA256.HashDataAsync(file)).ToLowerInvariant()!=ExpectedSha256)throw new InstallProblem("runtimeFailed");
                    using var process=Process.Start(new ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true,ArgumentList={"/silent","/install"}})??throw new InstallProblem("runtimeFailed");
                    using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    try{await process.WaitForExitAsync(deadline.Token);}catch(OperationCanceledException){try{process.Kill();}catch{}throw new InstallProblem("runtimeFailed");}
                    if(process.ExitCode!=0&&!Installed())throw new InstallProblem("runtimeFailed");
                }
            }finally{try{File.Delete(path);Directory.Delete(folder);}catch{}}
        });
    }
    internal static async Task EnsureCore(Func<bool> present,Func<Task> install)
    {
        if(present())return;
        try{await install();}catch{throw new InstallProblem("runtimeFailed");}
        if(!present())throw new InstallProblem("runtimeFailed");
    }
    internal static async Task<int> Exercise(string report)
    {
        if(!Path.IsPathFullyQualified(report)||File.Exists(report))return 12;
        var checks=new Dictionary<string,bool>();int calls=0;bool available=false;
        await EnsureCore(()=>true,()=>{calls++;return Task.CompletedTask;});checks["present_runtime_does_not_run_installer"]=calls==0;
        await EnsureCore(()=>available,()=>{calls++;available=true;return Task.CompletedTask;});checks["missing_runtime_installed_then_rechecked"]=calls==1&&available;
        try{await EnsureCore(()=>false,()=>Task.CompletedTask);checks["success_exit_without_runtime_rejected"]=false;}catch(InstallProblem p){checks["success_exit_without_runtime_rejected"]=p.Code=="runtimeFailed";}
        try{await EnsureCore(()=>false,()=>throw new IOException());checks["failed_prerequisite_blocks_app_replacement"]=false;}catch(InstallProblem p){checks["failed_prerequisite_blocks_app_replacement"]=p.Code=="runtimeFailed";}
        using var source=Assembly.GetExecutingAssembly().GetManifestResourceStream("WebViewBootstrapper29");
        checks["bundled_official_bootstrapper_matches_pinned_hash"]=source!=null&&Convert.ToHexString(await SHA256.HashDataAsync(source)).ToLowerInvariant()==ExpectedSha256;
        File.WriteAllText(report,JsonSerializer.Serialize(new{passed=checks.Values.All(v=>v),checks,realRuntimePresent=Installed(),runtimeInstallExecuted=false,ownerInstallationTouched=false},new JsonSerializerOptions{WriteIndented=true}));return checks.Values.All(v=>v)?0:1;
    }
}
