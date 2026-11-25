using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Rovunet.Core;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    const string HtmlOrigin29="https://rovunet.local";
    WebView2? html29;
    Grid? htmlHost29;
    bool htmlReady29,htmlStarting29,htmlPending29,htmlInactiveSent29;
    internal bool HtmlFixture29;
    string htmlLastModel29="";
    readonly Dictionary<long,FrameworkElement> htmlActions29=[];
    readonly Dictionary<long,FrameworkElement> htmlActionsPrev29=[];
    // A click can arrive for the model that was on screen a moment ago, while the
    // rebuild that follows a theme or page change has already replaced the ids.
    // Resolve those against the previous posting too instead of dropping them.
    FrameworkElement? HtmlAction29(long id)=>htmlActions29.TryGetValue(id,out var current)?current:htmlActionsPrev29.GetValueOrDefault(id);
    readonly ConditionalWeakTable<FrameworkElement,HtmlIdentity29> htmlIds29=new();
    long htmlNextId29;
    sealed record HtmlIdentity29(long Id);
    ContextMenu? htmlPopup29;
    readonly DispatcherTimer htmlPageTimer29=new(){Interval=TimeSpan.FromMilliseconds(500)};
    readonly DispatcherTimer htmlResize29=new(){Interval=TimeSpan.FromMilliseconds(180)};
    bool WantsHtml29=>!fixtureMode||HtmlFixture29;
    long HtmlId29(FrameworkElement control){long id=htmlIds29.GetValue(control,_=>new(++htmlNextId29)).Id;htmlActions29[id]=control;return id;}
    async Task StartHtml29()
    {
        if(!WantsHtml29||htmlStarting29||html29!=null||navigationShell==null)return;htmlStarting29=true;
        try{
            string dataFolder=fixtureMode?Path.Combine(htmlFixtureFolder29??throw new InvalidOperationException(),"webview-data"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Rovunet","WebView2");
            var native=navigationShell;Content=null;htmlHost29=new Grid();htmlHost29.Children.Add(native);
            html29=new WebView2{DefaultBackgroundColor=System.Drawing.Color.FromArgb(255,23,36,55)};htmlHost29.Children.Add(html29);Content=htmlHost29;
            var environment=await CoreWebView2Environment.CreateAsync(userDataFolder:dataFolder);
            await html29.EnsureCoreWebView2Async(environment);var core=html29.CoreWebView2;
            core.Settings.AreHostObjectsAllowed=false;core.Settings.AreDevToolsEnabled=false;core.Settings.AreDefaultContextMenusEnabled=false;
            core.Settings.AreDefaultScriptDialogsEnabled=false;core.Settings.IsStatusBarEnabled=false;core.Settings.IsZoomControlEnabled=false;
            core.Settings.IsGeneralAutofillEnabled=false;core.Settings.IsPasswordAutosaveEnabled=false;
            core.Settings.AreBrowserAcceleratorKeysEnabled=false;
            core.SetVirtualHostNameToFolderMapping("rovunet.local",Path.Combine(AppContext.BaseDirectory,"HtmlShell"),CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting+=(_,e)=>{if(e.Uri!=HtmlOrigin29+"/index.html")e.Cancel=true;};
            core.NewWindowRequested+=(_,e)=>e.Handled=true;
            core.PermissionRequested+=(_,e)=>e.State=CoreWebView2PermissionState.Deny;
            core.DownloadStarting+=(_,e)=>e.Cancel=true;
            core.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
            core.WebResourceRequested+=(_,e)=>{if(!Uri.TryCreate(e.Request.Uri,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Host!="rovunet.local")e.Response=environment.CreateWebResourceResponse(null,403,"Forbidden","");};
            core.WebMessageReceived+=HtmlMessage29;
            core.NavigationCompleted+=(_,e)=>{if(e.IsSuccess){htmlReady29=true;native.Opacity=0;native.IsHitTestVisible=false;native.Visibility=Visibility.Collapsed;MarqueeLine.RefreshAll(native);QueueHtml29();}else if(e.WebErrorStatus!=CoreWebView2WebErrorStatus.OperationCanceled)HtmlFailed29();};
            core.Navigate(HtmlOrigin29+"/index.html");
            Activated+=(_,_)=>{htmlInactiveSent29=false;htmlPageTimer29.Start();QueueHtml29();};Deactivated+=(_,_)=>{htmlPageTimer29.Stop();QueueHtml29();};
            // Maximising or restoring fires a burst of StateChanged events; one
            // model rebuild after the window settles avoids the visible stall.
            StateChanged+=(_,_)=>{htmlResize29.Stop();htmlResize29.Start();};
            htmlPageTimer29.Tick+=(_,_)=>{if(NavigationDepth>0&&IsActive)QueueHtml29();};if(IsActive)htmlPageTimer29.Start();
            htmlResize29.Tick+=(_,_)=>{htmlResize29.Stop();QueueHtml29();};
            Closed+=(_,_)=>{htmlPageTimer29.Stop();html29?.Dispose();};
        }catch{HtmlFailed29();}finally{htmlStarting29=false;}
    }
    // Native cards are the fallback view. While the HTML shell is live they are
    // updated lazily, so expanding or selecting inside a large collection never
    // rebuilds hundreds of hidden WPF tiles.
    bool nativeCardsDeferred29;
    bool nativeCardsKept29;
    void HtmlFailed29()
    {
        htmlReady29=false;
        if(navigationShell!=null){navigationShell.Visibility=Visibility.Visible;navigationShell.Opacity=1;navigationShell.IsHitTestVisible=true;}
        // Reveal the native tree before rebuilding it: a collapsed element
        // reports no width, and the rebuild reads one to pick compact or wide.
        if(nativeLayoutDeferred41) RebuildForResize();
        // Cards kept across a theme change carry the old palette; the native view
        // is the visible one now, so rebuild them fully.
        if(nativeCardsKept29){nativeCardsKept29=false;Build();}
        else if(nativeCardsDeferred29){nativeCardsDeferred29=false;RefreshCards();}
        RefreshHeroCollapse19();
        if(html29!=null)html29.Visibility=Visibility.Collapsed;
        Notify(L.Text("HTML_RUNTIME_REQUIRED"),Amber);
    }
    void QueueHtml29()
    {
        if(!htmlReady29||htmlPending29||closing||!fixtureMode&&!IsActive&&htmlInactiveSent29)return;htmlPending29=true;
        Dispatcher.BeginInvoke(()=>{htmlPending29=false;try{
            htmlActionsPrev29.Clear();foreach(var pair in htmlActions29)htmlActionsPrev29[pair.Key]=pair.Value;htmlActions29.Clear();var model=HtmlModel29();string json=JsonSerializer.Serialize(model);
            if(json!=htmlLastModel29){htmlLastModel29=json;html29!.CoreWebView2.PostWebMessageAsJson(json);}htmlInactiveSent29=!IsActive;
        }catch(Exception error){if(fixtureMode&&htmlFixtureFolder29!=null)File.AppendAllText(Path.Combine(htmlFixtureFolder29,"model-errors.txt"),error.ToString()+"\n");}},DispatcherPriority.Background);
    }
    void HtmlMessage29(object? sender,CoreWebView2WebMessageReceivedEventArgs args)
    {
        if(args.Source!=HtmlOrigin29+"/index.html"||closing)return;
        try{
            string json=args.WebMessageAsJson;if(json.Length>131072)return;
            using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=5});var message=doc.RootElement;
            string command=message.GetProperty("command").GetString()??"";
            string value=message.TryGetProperty("value",out var v)?v.GetString()??"":"";
            string id=message.TryGetProperty("id",out var p)?p.ToString():"";
            // Return from WebView2 before actions enter a modal page's DispatcherFrame.
            Dispatcher.BeginInvoke(()=>HtmlCommand30(command,id,value),DispatcherPriority.Normal);
        }catch(Exception error){HtmlCommandError30(error);}
    }
    void HtmlCommand30(string command,string id,string value)
    {
        if(closing)return;
        try{
            switch(command){
                case "ready":htmlLastModel29="";break;
                case "language":if(Languages.IsSupported(value)){htmlPopup29=null;ChangeLanguage(value);}break;
                case "language-picker":{
                    var menu=new ContextMenu();
                    foreach(var entry in Languages.All){
                        var item=new MenuItem{Header=entry.NativeName+(entry.Code==L.Language?" ✓":""),Tag="language:"+entry.Code};
                        var code=entry.Code;item.Click+=(_,e)=>{e.Handled=true;htmlPopup29=null;ChangeLanguage(code);};
                        menu.Items.Add(item);
                    }
                    htmlPopup29=HtmlMenu29(menu);
                }break;
                case "power":HeroConnect();break;
                case "search":homeSearchAction30.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));break;
                case "query":if(value.Length<=2000)search.Text=value;break;
                case "settings":SettingsDialog();break;
                case "general":GeneralPage();break;
                case "home":GoHome();break;
                case "profile":if(data.Profiles.FirstOrDefault(p=>p.Id==id) is {} profile)ActivateProfileCard20(profile);break;
                case "profile-menu":if(rows.FirstOrDefault(r=>r.Profile.Id==id) is {} row&&Visuals<System.Windows.Controls.Button>(row.Tile).FirstOrDefault(button=>Equals(button.Tag,"CARD_MORE"))?.ContextMenu is {} rowMenu)htmlPopup29=HtmlMenu29(rowMenu);break;
                case "group-menu":if(groups.FirstOrDefault(g=>g.Key==id) is {} group)htmlPopup29=HtmlMenu29(group.Menu);break;
                case "toggle":if(groups.FirstOrDefault(g=>g.Key==id) is {} toggle)ToggleCollection(toggle.Members);break;
                case "select-group":if(groups.FirstOrDefault(g=>g.Key==id) is {} pick)SelectCollection(pick.Members);break;
                case "ping":if(groups.FirstOrDefault(g=>g.Key==id) is {} ping)QuickPing29(ping.Members);break;
                case "group-power":if(groups.FirstOrDefault(g=>g.Key==id) is {} power)ConnectCollection(power.Members);break;
                case "stop":CancelCollectionTest();break;
                case "edit":if(data.Profiles.FirstOrDefault(p=>p.Id==id) is {} edit)Edit(edit);break;
                case "delete":if(data.Profiles.FirstOrDefault(p=>p.Id==id) is {} remove)Delete(remove);break;
                case "add":htmlPopup29=HtmlAddMenu29();break;
                case "close-menu":htmlPopup29=null;break;
                case "scan-select":
                    if(long.TryParse(id,out long listId)&&HtmlAction29(listId) is ListBox list){var parts=value.Split('|',2);if(parts.Length==2&&list.Items.OfType<ScanEntry>().FirstOrDefault(entry=>entry.Address==parts[1]) is {Selectable:true,Stale:false} entry){if(parts[0]=="true"){if(!list.SelectedItems.Contains(entry))list.SelectedItems.Add(entry);}else list.SelectedItems.Remove(entry);}}break;
                case "scan-page":if(long.TryParse(id,out long pageId)&&HtmlAction29(pageId) is ListBox paged&&int.TryParse(value,out int offset))htmlScanOffsets29[pageId]=Math.Clamp(offset,0,Math.Max(0,paged.Items.Count-1));break;
                case "native":
                    if(!long.TryParse(id,out var action)||HtmlAction29(action) is not {} target||!target.IsEnabled)return;
                    var openedMenu=htmlPopup29;htmlPopup29=null;
                    switch(target){
                        case MenuItem item:if(item.IsCheckable)item.IsChecked=!item.IsChecked;item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));if(item.StaysOpenOnClick)htmlPopup29=openedMenu;break;
                        case Button button when button.ContextMenu!=null:htmlPopup29=HtmlMenu29(button.ContextMenu);break;
                        case Button button:button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));break;
                        case RadioButton radio:radio.IsChecked=true;break;
                        case CheckBox check:check.IsChecked=value=="true";break;
                        case TextBox text when !text.IsReadOnly&&value.Length<=65536:text.Text=value;break;
                        case PasswordBox password when value.Length<=65536:password.Password=value;break;
                        case ComboBox combo when int.TryParse(value,out int index)&&index>=-1&&index<combo.Items.Count:combo.SelectedIndex=index;break;
                        case TabControl tabs when int.TryParse(value,out int tab)&&tab>=0&&tab<tabs.Items.Count:tabs.SelectedIndex=tab;break;
                        case Expander expander:expander.IsExpanded=value=="true";break;
                        case Slider slider when double.TryParse(value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double number)&&double.IsFinite(number):slider.Value=Math.Clamp(number,slider.Minimum,slider.Maximum);break;
                    }break;
            }
            QueueHtml29();
        }catch(Exception error){HtmlCommandError30(error);}
    }
    void HtmlCommandError30(Exception error){if(fixtureMode&&htmlFixtureFolder29!=null)File.AppendAllText(Path.Combine(htmlFixtureFolder29,"command-errors.txt"),error.ToString()+"\n");lastError=error is SafeError safe?safe.Code:"UNEXPECTED_ERROR";Notify(L.Text(lastError),Red);QueueHtml29();}
    void OpenOwnedMenu30(Button anchor,ContextMenu menu){if(htmlReady29){htmlPopup29=HtmlMenu29(menu);QueueHtml29();}else{menu.PlacementTarget=anchor;menu.IsOpen=true;}}
    ContextMenu HtmlAddMenu29()
    {
        var menu=new ContextMenu();menu.Items.Add(MenuAction("ADD_LINK_FILE",ImportDialog));menu.Items.Add(MenuAction("ADD_SUBSCRIPTION",SubscriptionDialog));menu.Items.Add(MenuAction("ADD_MANUAL",NewProfileSelection));menu.Items.Add(MenuAction("PUBLIC_SOURCES",PublicSourcesPage));menu.Items.Add(MenuAction("CHAIN_TITLE",()=>BuildChainEditor25().ShowDialog()));return HtmlMenu29(menu);
    }
    internal string? htmlFixtureFolder29;
}
