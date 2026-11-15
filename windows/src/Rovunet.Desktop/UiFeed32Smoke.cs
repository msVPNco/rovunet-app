using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;
using Rovunet.Platform;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    internal async Task<bool> RunFeed32(string report)
    {
        string folder=Path.GetDirectoryName(report)!,phase="fetch";Directory.CreateDirectory(folder);
        var checks=new Dictionary<string,bool>();var proxyBefore=new WinInetProxy().Read();int pointers=0,saves=0;
        const string first="socks5://127.0.0.1:9#Mahsa-new",second="socks5://127.0.0.1:10#Mahsa-second",old="socks5://127.0.0.1:11#Mahsa-old";
        static string Cipher(params string[] links){using var aes=Aes.Create();aes.Key=Encoding.ASCII.GetBytes("ca815ecfdb1f153a");return Convert.ToBase64String(aes.EncryptCbc(JsonSerializer.SerializeToUtf8Bytes(new{mci=links.Select(config=>new{config,ads_url="https://ads.example.invalid/never"}),mtn=Array.Empty<object>()}),Encoding.ASCII.GetBytes("lvcas56410c97lpb"),PaddingMode.PKCS7));}
        string cipher=Cipher(first,second),source=Importer.Fingerprint(PublicFeed.MahsaUrl);var requested=new List<string>();var ports=new List<int>();var headers=new List<(string User,string Accept)>();
        string Snapshot()=>JsonSerializer.Serialize(new{data.Profiles,data.Subscriptions,data.SelectedId,data.LocalOverrideNames,data.LocalOverrideSources,data.SlipTrustPins,data.SshKeys,data.SubscriptionUpdated});
        static HttpResponseMessage Response(HttpStatusCode code,string body="")=>new(code){Content=new StringContent(body)};
        void Http(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> respond){requested.Clear();ports.Clear();headers.Clear();SubscriptionClient.HttpFixture=port=>{ports.Add(port);return new UpdateFixtureHandler28((request,stop)=>{requested.Add(request.RequestUri!.AbsoluteUri);headers.Add((request.Headers.UserAgent.ToString(),request.Headers.Accept.ToString()));return respond(request,stop);});};}
        bool OfficialOnly()=>requested.All(u=>u is PublicFeed.MahsaUrl or PublicFeed.MahsaApi32);
        async Task<bool> Wait(string expression){for(int i=0;i<150;i++){if(htmlReady29&&await html29!.CoreWebView2.ExecuteScriptAsync("Boolean("+expression+")").WaitAsync(TimeSpan.FromSeconds(4))=="true")return true;await Task.Delay(30);}return false;}
        string Find(string label)=>"[...document.querySelectorAll('.native-page button')].find(b=>b.getClientRects().length&&b.getAttribute('aria-label')==="+JsonSerializer.Serialize(label)+")";
        async Task Click(string expression){
            if(!await Wait(expression+"!=null"))throw new InvalidOperationException("Missing "+phase);
            await html29!.CoreWebView2.ExecuteScriptAsync("("+expression+").scrollIntoView({block:'center',inline:'nearest'})");await Task.Delay(35);
            using var point=JsonDocument.Parse(await html29.CoreWebView2.ExecuteScriptAsync("(()=>{const b="+expression+",r=b.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return{x,y,hit:b.contains(document.elementFromPoint(x,y)),disabled:b.disabled===true}})()"));var p=point.RootElement;
            if(!p.GetProperty("hit").GetBoolean()||p.GetProperty("disabled").GetBoolean())throw new InvalidOperationException("Obscured "+phase);
            foreach(string type in new[]{"mousePressed","mouseReleased"})await html29.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type,x=p.GetProperty("x").GetDouble(),y=p.GetProperty("y").GetDouble(),button="left",clickCount=1}));pointers++;
        }
        try{
            storeSaveFixture28=()=>saves++;
            Http((_,_)=>Task.FromResult(Response(HttpStatusCode.OK,cipher)));
            var body=await PublicFeed.Fetch(PublicFeed.MahsaUrl,proxyPort:17291);
            checks["raw_primary_success_does_not_contact_api"]=requested.SequenceEqual(new[]{PublicFeed.MahsaUrl})&&body.Body==first+"\n"+second&&ports.SequenceEqual(new[]{17291});
            checks["ordinary_raw_request_does_not_receive_api_accept_header"]=headers.Single().Accept=="";
            foreach(string failure in new[]{"socket","404"}){
                Http((request,_)=>request.RequestUri!.AbsoluteUri==PublicFeed.MahsaUrl?failure=="404"?Task.FromResult(Response(HttpStatusCode.NotFound)):Task.FromException<HttpResponseMessage>(new HttpRequestException("synthetic reset")):Task.FromResult(Response(HttpStatusCode.OK,cipher)));
                body=await PublicFeed.Fetch(PublicFeed.MahsaUrl,proxyPort:17292);
                checks[failure+"_uses_exact_official_file_and_keeps_proxy_route"]=requested.SequenceEqual(new[]{PublicFeed.MahsaUrl,PublicFeed.MahsaApi32})&&ports.SequenceEqual(new[]{17292,17292})&&body.Body==first+"\n"+second;
                checks[failure+"_api_requests_raw_content_with_user_agent"]=headers[1] is{User:"Rovunet-Windows",Accept:"application/vnd.github.raw+json"}&&OfficialOnly();
            }
            Http((_,_)=>Task.FromResult(Response(HttpStatusCode.OK,"invalid ciphertext")));
            try{await PublicFeed.Fetch(PublicFeed.MahsaUrl);checks["decode_failure_not_hidden_by_fallback"]=false;}catch(SafeError e){checks["decode_failure_not_hidden_by_fallback"]=e.Code=="MAHSA_DECODE_FAILED"&&requested.Count==1;}
            Http((request,_)=>Task.FromResult(Response(request.RequestUri!.AbsoluteUri==PublicFeed.MahsaUrl?HttpStatusCode.BadGateway:HttpStatusCode.Found)));
            try{await PublicFeed.Fetch(PublicFeed.MahsaUrl);checks["failed_api_or_redirect_has_no_third_party_fallback"]=false;}catch(SafeError e){checks["failed_api_or_redirect_has_no_third_party_fallback"]=e.Code=="FETCH_FAILED"&&requested.Count==2&&OfficialOnly();}
            Http((_,stop)=>{stop.ThrowIfCancellationRequested();return Task.FromResult(Response(HttpStatusCode.OK,cipher));});
            using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();try{await PublicFeed.Fetch(PublicFeed.MahsaUrl,cancelled.Token);checks["user_cancel_does_not_try_api"]=false;}catch(Exception e)when(e is SafeError{Code:"CANCELLED"} or OperationCanceledException){checks["user_cancel_does_not_try_api"]=!requested.Contains(PublicFeed.MahsaApi32);}}
            Http((_,_)=>Task.FromResult(Response(HttpStatusCode.OK,cipher)));
            try{await SubscriptionClient.Fetch(new Uri("https://ads.example.invalid/never"),githubRaw32:true);checks["api_headers_cannot_be_enabled_for_another_url"]=false;}catch(SafeError e){checks["api_headers_cannot_be_enabled_for_another_url"]=e.Code=="INVALID_IMPORT"&&requested.Count==0;}
            var manual=Importer.Single("socks5://127.0.0.1:12#Manual");var other=Importer.Single("socks5://127.0.0.1:13#Unrelated");other.SourceId="unrelated";
            var saved=Importer.Single(old);saved.SourceId=source;saved.CollectionId="sub:"+source;saved.ItemId=Importer.Fingerprint(saved.Raw);
            data.Profiles=[saved,manual,other];data.Subscriptions=[new(){Id=source,Name="Owner Mahsa",PublicSource="mahsa",Url="https://raw.githubusercontent.com/hiddify/hiddify-app/main/test.configs/mahsa"},new(){Id="unrelated",Name="Other",Url="https://other.example.invalid/sub"}];data.SelectedId=saved.Id;
            string before=Snapshot();int beforeSaves=saves;
            Http((_,_)=>Task.FromResult(Response(HttpStatusCode.BadGateway)));await UpdateCollection20(source);
            checks["both_routes_fail_preserves_all_old_data_and_never_saves"]=Snapshot()==before&&saves==beforeSaves&&requested.SequenceEqual(new[]{PublicFeed.MahsaUrl,PublicFeed.MahsaApi32});
            Http((request,_)=>Task.FromResult(Response(request.RequestUri!.AbsoluteUri==PublicFeed.MahsaUrl?HttpStatusCode.BadGateway:HttpStatusCode.OK,cipher)));await UpdateCollection20(source);
            checks["successful_api_update_replaces_only_mahsa"]=data.Profiles.Count(p=>p.SourceId==source)==2&&!data.Profiles.Any(p=>p.Raw==old)&&data.Profiles.Contains(manual)&&data.Profiles.Contains(other)&&data.Subscriptions.Count==2&&data.Subscriptions.Single(s=>s.Id==source).Name=="Owner Mahsa";
            checks["legacy_hiddify_address_is_never_requested"]=requested.SequenceEqual(new[]{PublicFeed.MahsaUrl,PublicFeed.MahsaApi32});
            before=Snapshot();beforeSaves=saves;storeSaveFixture28=()=>throw new SafeError("PRIVATE_STORE_FAILED");await UpdateCollection20(source);
            checks["save_failure_rolls_back_semantic_data"]=Snapshot()==before;storeSaveFixture28=()=>saves++;
            await StartHtml29();Activate();html29!.Focus();
            foreach(string language in new[]{"en","fa","ru","zh-CN"})foreach(string theme in new[]{"light","dark","amoled"}){
                phase=language+"_"+theme;L.Language=data.Language=language;data.Theme=theme;Width=480;Height=900;data.Profiles=[manual,other];data.Subscriptions=[];data.SelectedId="";ApplyTheme();Build();
                Http((request,_)=>Task.FromResult(Response(request.RequestUri!.AbsoluteUri==PublicFeed.MahsaUrl?HttpStatusCode.BadGateway:HttpStatusCode.OK,Cipher(first,"future-protocol://not-yet-supported"))));
                var page=BuildSubscriptionPage28(initialUrl:PublicFeed.MahsaUrl);page.Show();Visuals<TextBox>(page.PageVisual).Single(t=>Equals(t.Tag,"SUBSCRIPTION_NAME")).Text="Mahsa fixture";QueueHtml29();
                await Click("document.querySelector('.native-page input[type=checkbox]')");await Click(Find(L.Text("IMPORT")));
                checks[phase+"_first_add_fetches_and_reviews_without_saving"]=await Wait("document.querySelector('.native-page')?.textContent.includes("+JsonSerializer.Serialize(L.Text("PUBLIC_PARTIAL_READY"))+")")&&data.Profiles.Count==2&&OfficialOnly()&&requested.Count==2;
                checks[phase+"_managed_url_stays_hidden_in_rendered_inputs"]=await Wait("[...document.querySelectorAll('.native-page input,.native-page textarea')].every(e=>!e.value.includes('raw.githubusercontent.com')&&!e.value.includes('api.github.com'))");
                await Click(Find(L.Text("IMPORT")));
                checks[phase+"_second_add_keeps_unrelated_profiles"]=await Wait("document.querySelector('.hero')!==null")&&data.Profiles.Count==3&&data.Profiles.Contains(manual)&&data.Profiles.Contains(other)&&data.Subscriptions.Single().PublicSource=="mahsa";
                var details=BuildSubscriptionDetails28(data.Subscriptions.Single());details.Show();QueueHtml29();
                checks[phase+"_saved_details_offer_no_raw_url_copy"]=await Wait("document.querySelector('.native-page')!==null")&&!Visuals<TextBox>(details.PageVisual).Any(t=>Equals(t.Tag,"SAVED_SUBSCRIPTION_URL"))&&!Visuals<Button>(details.PageVisual).Any(b=>System.Windows.Automation.AutomationProperties.GetName(b)==L.Text("COPY"));details.Close();
            }
            checks["owner_network_and_agents_untouched"]=proxyBefore==new WinInetProxy().Read()&&client==null&&testing==null;
        }catch(Exception e){checks["exception_at_"+phase]=false;File.WriteAllText(report+".error",e.ToString());}
        finally{foreach(var page in DialogWindow.OpenPages.Where(p=>p.PageHost==this).Reverse().ToArray())page.Close();SubscriptionClient.HttpFixture=null;storeSaveFixture28=null;htmlPageTimer29.Stop();durationTimer.Stop();routingTimer28.Stop();}
        bool passed=checks.Count>0&&checks.Values.All(v=>v);File.WriteAllText(report,JsonSerializer.Serialize(new{passed,checks,actualPointerClicks=pointers,syntheticFeed=true,realNetwork=false,advertisementsFetched=false,ownerStoreTouched=false},new JsonSerializerOptions{WriteIndented=true}));return passed;
    }
}
