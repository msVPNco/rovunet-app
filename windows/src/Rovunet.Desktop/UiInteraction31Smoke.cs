using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunInteraction31(string report)
    {
        var checks=new Dictionary<string,bool>();var geometry=new List<object>();string phase="start",folder=Path.GetDirectoryName(report)!;Directory.CreateDirectory(folder);var proxy=new WinInetProxy().Read();int pointers=0;
        try{
            storeSaveFixture28=()=>{};L.Language=data.Language="en";data.Theme="dark";Width=480;Height=900;ApplyTheme();
            data.Profiles=Enumerable.Range(0,18).Select(i=>{var p=Importer.Single("hy2://fixture@127.0.0.1:19499#Path-"+i);p.CollectionId="interaction31";p.CollectionName="Interaction";return p;}).ToList();data.SelectedId=data.Profiles[0].Id;data.OpenCollectionId="interaction31";data.CollectionExpansionRevision=1;
            Build();await StartHtml29();Activate();html29!.Focus();
            async Task<bool> Wait(string expression){for(int i=0;i<150;i++){if(htmlReady29&&await html29.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(3))=="true")return true;await Task.Delay(30);}return false;}
            async Task Click(string expression){if(!await Wait(expression+"!=null"))throw new InvalidOperationException("Missing at "+phase);await html29.CoreWebView2.ExecuteScriptAsync("("+expression+").scrollIntoView({block:'nearest'})");await Task.Delay(50);using var json=JsonDocument.Parse(await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const b="+expression+",r=b.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return {x,y,hit:b.contains(document.elementFromPoint(x,y)),disabled:b.disabled}})()"));var at=json.RootElement;if(!at.GetProperty("hit").GetBoolean()||at.GetProperty("disabled").GetBoolean())throw new InvalidOperationException("Unreachable at "+phase);foreach(string type in new[]{"mousePressed","mouseReleased"})await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type,x=at.GetProperty("x").GetDouble(),y=at.GetProperty("y").GetDouble(),button="left",clickCount=1}));pointers++;}
            checks["ready"]=await Wait("document.querySelectorAll('.profile').length===18");
            phase="collapse";
            // Ignore external mouse-wheel input only in this fixture while frame
            // sampling. Programmatic scrolling still exercises the real layout.
            await html29.CoreWebView2.ExecuteScriptAsync("window.fixtureWheel31=e=>e.preventDefault();document.addEventListener('wheel',window.fixtureWheel31,{capture:true,passive:false})");
            foreach(string language in new[]{"en","fa"})foreach(int width in new[]{480,1120}){
                L.Language=data.Language=language;Width=width;Build();QueueHtml29();await Wait("document.documentElement.lang==="+JsonSerializer.Serialize(language));
                double? height=null;bool stable=true,inside=true,range=true,anchored=true;
                foreach(int target in new[]{0,10,25,40,55,59,60,61,65,90,150,600,65,60,59,40,10,0}){
                    string script="new Promise(resolve=>{document.querySelector('.home-scroll').scrollTop="+target+";const samples=[];let n=0;function frame(){const s=document.querySelector('.home-scroll'),h=s.querySelector('.hero'),r=h.getBoundingClientRect(),p=h.querySelector('.power-frame').getBoundingClientRect(),t=h.querySelector('.hero-title').getBoundingClientRect();samples.push({top:s.scrollTop,height:r.height,extent:s.scrollHeight,inside:p.top>=r.top-1&&p.bottom<=r.bottom+1&&t.top>=r.top-1&&t.bottom<=r.bottom+1,anchor:r.top-s.getBoundingClientRect().top});if(++n<24)requestAnimationFrame(frame);else resolve(samples)}requestAnimationFrame(frame)})";
                    using var samples=JsonDocument.Parse(await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Runtime.evaluate",JsonSerializer.Serialize(new{expression=script,awaitPromise=true,returnByValue=true})));var all=samples.RootElement.GetProperty("result").GetProperty("value").EnumerateArray().ToArray();var tail=all.Skip(12).ToArray();geometry.Add(new{language,width,target,samples=all.Select(s=>s.Clone()).ToArray()});
                    double extent=all[^1].GetProperty("extent").GetDouble();height??=extent;stable&=Math.Abs(extent-height.Value)<1&&tail.Max(s=>s.GetProperty("top").GetDouble())-tail.Min(s=>s.GetProperty("top").GetDouble())<1&&tail.Max(s=>s.GetProperty("height").GetDouble())-tail.Min(s=>s.GetProperty("height").GetDouble())<1;
                    inside&=tail.All(s=>s.GetProperty("inside").GetBoolean());range&=tail.All(s=>s.GetProperty("height").GetDouble() is >=87 and <=149);anchored&=tail.All(s=>Math.Abs(s.GetProperty("anchor").GetDouble())<1);
                }
                checks[language+width+"_stable_scroll_extent_no_feedback"]=stable;checks[language+width+"_button_title_inside"]=inside;checks[language+width+"_height_range"]=range;checks[language+width+"_sticky_anchor"]=anchored;
            }
            await html29.CoreWebView2.ExecuteScriptAsync("document.removeEventListener('wheel',window.fixtureWheel31,true)");
            phase="stop";L.Language=data.Language="en";Width=480;Build();QueueHtml29();await Wait("document.documentElement.lang==='en'");
            var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);handshakeFixture30=async(_,plan,stop)=>{gate.TrySetResult();await Task.Delay(Timeout.Infinite,stop);return new(false,0,"CANCELLED",plan.Mechanism);};
            string test="document.querySelector('[data-test=quick-ping]')";await Click(test);await gate.Task.WaitAsync(TimeSpan.FromSeconds(5));checks["ping_becomes_stop_while_probe_running"]=await Wait(test+"?.getAttribute('aria-label')==="+JsonSerializer.Serialize(L.Text("STOP_TEST"))+"&&"+test+"?.getAttribute('aria-busy')==='true'");
            checks["stop_has_distinct_visible_danger_state"]=await Wait(test+"?.dataset.state==='stop'&&"+test+"?.classList.contains('test-button')");
            using(var image=File.Create(Path.Combine(folder,"ping-stop-dark.png")))await html29.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,image);
            var clock=Stopwatch.StartNew();await Click(test);await collectionTask!.WaitAsync(TimeSpan.FromSeconds(3));checks["same_button_cancels_quick_probe_promptly"]=clock.ElapsedMilliseconds<1500&&currentCollection is{Cancelled:true,Running:false};checks["button_returns_to_ping_after_cancel"]=await Wait(test+"?.getAttribute('aria-label')==="+JsonSerializer.Serialize(L.Text("QUICK_PING"))+"&&"+test+"?.getAttribute('aria-busy')==='false'");checks["cancelled_quick_probe_does_not_write_https_results"]=data.Measurements.Count==0;
            using(var image=File.Create(Path.Combine(folder,"stopped-dark.png")))await html29.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,image);
            foreach(string language in new[]{"fa","en","ru","zh-CN"})foreach(string theme in new[]{"dark","light","amoled"}){
                phase="stop_"+language+theme;L.Language=data.Language=language;data.Theme=theme;ApplyTheme();Build();QueueHtml29();await Wait("document.documentElement.lang==="+JsonSerializer.Serialize(language)+"&&document.body.dataset.theme==="+JsonSerializer.Serialize(theme));gate=new(TaskCreationOptions.RunContinuationsAsynchronously);
                await Click(test);await gate.Task.WaitAsync(TimeSpan.FromSeconds(5));checks[phase+"_visible_localized_stop"]=await Wait(test+"?.dataset.state==='stop'&&"+test+"?.getAttribute('aria-label')==="+JsonSerializer.Serialize(L.Text("STOP_TEST")));
                await Click(test);await collectionTask!.WaitAsync(TimeSpan.FromSeconds(3));checks[phase+"_cancels_and_returns_to_ping"]=await Wait(test+"?.dataset.state==='ping'&&"+test+"?.getAttribute('aria-label')==="+JsonSerializer.Serialize(L.Text("QUICK_PING")));
            }
            checks["fixture_creates_no_agent_or_system_proxy_request"]=client==null&&testing==null&&!data.UseSystemProxy;
        }catch(Exception error){checks["exception_at_"+phase]=false;File.WriteAllText(report+".error",error.ToString());}
        finally{CancelCollectionTest();htmlPageTimer29.Stop();durationTimer.Stop();routingTimer28.Stop();handshakeFixture30=null;storeSaveFixture28=null;}
        File.WriteAllText(Path.Combine(folder,"scroll-samples.json"),JsonSerializer.Serialize(geometry));bool passed=checks.Count>0&&checks.Values.All(x=>x);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,actualPointerClicks=pointers,liveRemoteNetwork=false,ownerStoreTouched=false,systemProxyObservedUnchanged=proxy==new WinInetProxy().Read(),systemProxyObservationScope="External/OS changes may occur during frame sampling; the fixture starts no Agent and requests no system proxy."},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
