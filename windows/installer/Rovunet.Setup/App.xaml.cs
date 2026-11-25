using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rovunet.Setup;
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Windows 10 1809 (build 17763) is the floor for the bundled .NET 10 runtime.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10,0,17763) || !Environment.Is64BitProcess) { Shutdown(30);return; }
        // Elevated helper only installs the embedded payload; never loads or launches user profiles.
        if(e.Args is ["--install-elevated",var target,var desktop,var test])
        {
            if(!InstallService.Elevated || desktop is not("0" or "1") || test is not("0" or "1")){Shutdown(44);return;}
            InstallService.Testing=test=="1";
            try{await InstallService.InstallLocal(target,desktop=="1");Shutdown();}
            catch(InstallProblem p){Shutdown(p.Code=="appRunning"?41:p.Code=="unknownFolder"?43:p.Code=="invalidPath"?45:p.Code=="rollbackFailed"?46:42);}catch{Shutdown(42);}return;
        }
        if (e.Args is ["--exercise",var report]) { InstallService.Testing=true;Shutdown(await SetupExercise.Run(report));return; }
        if (e.Args is ["--runtime-exercise",var runtimeReport]) { Shutdown(await WebViewRuntime29.Exercise(runtimeReport));return; }
        if (e.Args is ["--preview",var preview])
        {
            if(!Path.IsPathFullyQualified(preview)||File.Exists(preview)){Shutdown(12);return;}
            try
            {
                var checks=new Dictionary<string,bool>();
                foreach(string lang in new[]{"fa","en","ru","zh-CN"})foreach(bool dark in new[]{false,true})
                {
                    var window=new SetupWindow(lang,dark,true);window.Show();window.UpdateLayout();checks[$"launch_default_{lang}_{dark}"]=window.LaunchChoice.IsChecked==!InstallService.Elevated;checks[$"compact_window_{lang}_{dark}"]=window.Width<=520&&window.Height<=680;
                    if(lang=="fa")checks[$"bundled_persian_font_resolves_{dark}"]=new Typeface(window.FontFamily,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal).TryGetGlyphTypeface(out var glyph)&&glyph.FamilyNames.Values.Any(name=>name=="Vazirmatn");
                    SaveImage((FrameworkElement)window.Content,Path.Combine(Path.GetDirectoryName(preview)!, $"setup-{lang}-{(dark?"dark":"light")}.png"));
                    checks[$"preview_never_installs_{lang}_{dark}"]=!window.InstallButton.IsEnabled;
                    checks[$"touch_target_{lang}_{dark}"]=window.InstallButton.ActualHeight>=48;
                    var choice=window.DesktopChoice;checks[$"desktop_default_checked_{lang}_{dark}"]=choice.IsChecked==true;checks[$"desktop_enabled_{lang}_{dark}"]=choice.IsEnabled;
                    var hit=VisualTreeHelper.HitTest(choice,new Point(choice.ActualWidth/2,choice.ActualHeight/2))?.VisualHit;while(hit!=null&&hit is not CheckBox)hit=VisualTreeHelper.GetParent(hit);checks[$"desktop_whole_row_hit_target_{lang}_{dark}"]=ReferenceEquals(hit,choice);
                    var peer=new System.Windows.Automation.Peers.CheckBoxAutomationPeer(choice);var toggle=(System.Windows.Automation.Provider.IToggleProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Toggle);toggle.Toggle();bool off=choice.IsChecked==false;toggle.Toggle();checks[$"desktop_toggle_works_{lang}_{dark}"]=off&&choice.IsChecked==true;window.Close();
                }
                File.WriteAllText(preview,JsonSerializer.Serialize(new{fixture="visual preview only; no installation or registration",checks,passed=checks.Values.All(v=>v)},new JsonSerializerOptions{WriteIndented=true}));Shutdown(checks.Values.All(v=>v)?0:1);
            }catch{Shutdown(1);}return;
        }
        new SetupWindow("fa",true).Show();
    }
    internal static void SaveImage(FrameworkElement visual,string path)
    {
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth),(int)Math.Ceiling(visual.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var stream=new FileStream(path,FileMode.CreateNew);png.Save(stream);
    }
}
public sealed class SetupWindow : Window
{
    public Button InstallButton { get; }
    internal CheckBox DesktopChoice {get;}
    internal CheckBox LaunchChoice {get;}
    public TaskCompletionSource<bool> Installation { get; } = new();
    internal bool Completed { get;private set; }
    public SetupWindow(string language,bool dark,bool fixture=false,string? chosenPath=null,bool desktop=true,bool launch=true,bool closeAppWhenLastWindow=true)
    {
        var assembly=Assembly.GetExecutingAssembly();using var stream=assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(n=>n.EndsWith($".Locales.{language}.json")))!;var strings=JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;string T(string key)=>strings[key];
        Brush ColorBrush(string value)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        Brush ink=ColorBrush(dark?"#ECEEF3":"#16181D"),bg=ColorBrush(dark?"#0E1014":"#F6F7F9"),card=ColorBrush(dark?"#171B22":"#FFFFFF"),muted=ColorBrush(dark?"#9BA3B2":"#5A6472"),accent=ColorBrush(dark?"#B3A6FF":"#5B4EA8"),line=ColorBrush(dark?"#2A303B":"#E4E7EE");
        Resources["SetupInk"]=ink;Resources["SetupBg"]=bg;Resources["SetupCard"]=card;
        Title="Rovunet · "+T("title");Width=Math.Min(520,SystemParameters.WorkArea.Width-24);Height=Math.Min(640,SystemParameters.WorkArea.Height-24);MinWidth=Math.Min(460,Width);MinHeight=Math.Min(540,Height);ResizeMode=ResizeMode.NoResize;
        FontFamily=language=="fa"?new FontFamily(new Uri($"pack://application:,,,/{assembly.GetName().Name};component/"),"./Assets/#Vazirmatn"):new FontFamily(language=="zh-CN"?"Microsoft YaHei UI":"Segoe UI");FontSize=14;Foreground=ink;Background=bg;FlowDirection=FlowDirection.LeftToRight;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        if(!fixture&&closeAppWhenLastWindow)Closed+=(_,_)=>Dispatcher.BeginInvoke(()=>{if(Application.Current.Windows.Count==0)Application.Current.Shutdown();});
        // Everything fits in one window: header, location, options and footer.
        // The scroll viewer remains only as a fallback for very short screens.
        var root=new DockPanel{FlowDirection=language=="fa"?FlowDirection.RightToLeft:FlowDirection.LeftToRight,Background=bg};
        var footer=new DockPanel{Margin=new(20,4,20,16),FlowDirection=language=="fa"?FlowDirection.RightToLeft:FlowDirection.LeftToRight};DockPanel.SetDock(footer,Dock.Bottom);
        var close=new Button{Content=T("close"),MinWidth=110};close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Right);footer.Children.Add(close);
        InstallButton=new Button{Content=T("install"),IsEnabled=InstallService.Available&&!fixture,Background=accent,Foreground=ColorBrush(dark?"#0B0912":"#FFFFFF"),MinWidth=170,MinHeight=48,HorizontalAlignment=HorizontalAlignment.Left,IsDefault=true};footer.Children.Add(InstallButton);root.Children.Add(footer);
        var content=new StackPanel{Margin=new(20,8,20,4)};
        TextBlock Text(string value,double size=14,Brush? color=null)=>new(){Text=value,FontSize=size,Foreground=color??ink,Margin=new(0,1,0,4)};
        void Card(StackPanel body)=>content.Children.Add(new Border{Background=card,CornerRadius=new(14),Padding=new(12,10,12,10),Margin=new(0,3,0,6),BorderBrush=line,BorderThickness=new(1),Child=body});
        // Header: brand and intro on the reading side, language and theme on the other.
        var head=new Grid{Margin=new(0,2,0,6)};
        head.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
        head.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        head.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var brandRow=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
        brandRow.Children.Add(Text("Rovunet",26));brandRow.Children.Add(Text(T("intro"),13,muted));
        var preferences=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
        var languages=new ComboBox{MinHeight=42,MinWidth=130,Margin=new(4,0,4,0),VerticalContentAlignment=VerticalAlignment.Center,ItemsSource=new[]{"فارسی","English","Русский","简体中文"},SelectedIndex=Array.IndexOf(new[]{"fa","en","ru","zh-CN"},language)};
        var theme=new Button{Content=T(dark?"lightTheme":"darkTheme"),MinHeight=42,MinWidth=110,Margin=new(4,0,0,0)};preferences.Children.Add(languages);preferences.Children.Add(theme);
        head.Children.Add(brandRow);Grid.SetColumn(preferences,1);head.Children.Add(preferences);content.Children.Add(head);
        if(fixture)content.Children.Add(Text(T("preview"),12,muted));
        var location=new StackPanel();
        var locationTitle=new DockPanel();var locationLabel=Text(T("location"),16);DockPanel.SetDock(locationLabel,Dock.Left);locationTitle.Children.Add(locationLabel);location.Children.Add(locationTitle);
        var path=new TextBox{Text=chosenPath??InstallService.ProgramFilesTarget,FlowDirection=FlowDirection.LeftToRight,Background=bg,Foreground=ink,Tag="INSTALL_PATH",MinHeight=44};
        var browse=new Button{Content="…",MinWidth=52,MinHeight=44,Margin=new(8,0,0,0),ToolTip=T("browse"),Tag="BROWSE"};
        browse.Click+=(_,_)=>{var picker=new Microsoft.Win32.OpenFolderDialog();if(picker.ShowDialog(this)==true)path.Text=new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}.Contains(picker.FolderName,StringComparer.OrdinalIgnoreCase)?Path.Combine(picker.FolderName,"Rovunet"):picker.FolderName;};
        var pathRow=new DockPanel{LastChildFill=true};DockPanel.SetDock(browse,Dock.Right);pathRow.Children.Add(browse);pathRow.Children.Add(path);location.Children.Add(pathRow);
        string? previous=fixture?null:InstallService.RegisteredPaths().FirstOrDefault();
        if(previous!=null){var upgrade=new Button{Content=T("upgrade"),HorizontalAlignment=HorizontalAlignment.Left,MinHeight=44,Margin=new(0,6,0,0),Tag="CURRENT_FOLDER"};upgrade.Click+=(_,_)=>path.Text=previous;location.Children.Add(upgrade);}
        location.Children.Add(Text(T("private"),12,muted));location.Children.Add(Text(T("admin"),12,muted));Card(location);
        var options=new StackPanel();
        var desktopChoice=DesktopChoice=new CheckBox{Foreground=ink,Content=T("shortcut"),IsChecked=desktop,Tag="DESKTOP_SHORTCUT",MinHeight=44};
        var launchChoice=LaunchChoice=new CheckBox{Foreground=ink,Content=T("launch"),IsChecked=launch&&!InstallService.Elevated,IsEnabled=!InstallService.Elevated,ToolTip=InstallService.Elevated?T("launchUser"):null,MinHeight=44};
        var license=new Button{Content=T("license"),HorizontalAlignment=HorizontalAlignment.Left,MinHeight=40,Margin=new(0,1,0,0)};
        license.Click+=(_,_)=>{using var source=assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(n=>n.EndsWith(".LICENSE")))!;using var reader=new StreamReader(source);new Window{Title=T("license"),Owner=this,Width=500,Height=650,Content=new TextBox{Text=reader.ReadToEnd(),IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,FlowDirection=FlowDirection.LeftToRight},WindowStartupLocation=WindowStartupLocation.CenterOwner}.ShowDialog();};
        options.Children.Add(desktopChoice);options.Children.Add(launchChoice);options.Children.Add(license);Card(options);
        var status=Text("",12,muted);status.Tag="INSTALL_STATUS";content.Children.Add(status);var progress=new ProgressBar{Height=4,IsIndeterminate=true,Visibility=Visibility.Collapsed};content.Children.Add(progress);
        var scroll=new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,PanningMode=PanningMode.VerticalOnly};root.Children.Add(scroll);Content=root;
        void Switch(string lang,bool nextDark){new SetupWindow(lang,nextDark,fixture,path.Text,desktopChoice.IsChecked==true,launchChoice.IsChecked==true).Show();Close();}
        languages.SelectionChanged+=(_,_)=>Switch(new[]{"fa","en","ru","zh-CN"}[languages.SelectedIndex],dark);theme.Click+=(_,_)=>Switch(language,!dark);
        bool installing=false;Closing+=(_,args)=>{if(installing)args.Cancel=true;};
        InstallButton.Click+=async(_,_)=>{
            if(Completed){if(launchChoice.IsChecked==true&&!InstallService.Elevated)try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(path.Text,"Rovunet.exe")){UseShellExecute=true});}catch{}Close();return;}
            if(fixture||installing||!InstallService.Available)return;
            bool succeeded=false;installing=true;InstallButton.IsEnabled=close.IsEnabled=content.IsEnabled=false;progress.Visibility=Visibility.Visible;status.Text=T("installing");status.BringIntoView();
            try
            {
                await WebViewRuntime29.Ensure(()=>status.Text=T("runtimeInstalling"));
                status.Text=T("installing");await InstallService.Install(path.Text,desktopChoice.IsChecked==true);succeeded=Completed=true;
                var success=new StackPanel{Margin=new(24,48,24,32),VerticalAlignment=VerticalAlignment.Center};success.Children.Add(new TextBlock{Text="✓",FontSize=72,Foreground=ColorBrush(dark?"#8DDBB6":"#196844"),HorizontalAlignment=HorizontalAlignment.Center});success.Children.Add(Text(T("success"),26));success.Children.Add(Text(T("successHint")));success.Children.Add(new TextBlock{Text=path.Text,TextWrapping=TextWrapping.Wrap,FlowDirection=FlowDirection.LeftToRight,Foreground=ink,Margin=new(0,18,0,18)});scroll.Content=success;scroll.ScrollToTop();InstallButton.Content=T("finish");close.Visibility=Visibility.Collapsed;
            }
            catch(Exception error){status.Text=T(error is InstallProblem p&&strings.ContainsKey(p.Code)?p.Code:"failed");}
            finally{installing=false;progress.Visibility=Visibility.Collapsed;content.IsEnabled=close.IsEnabled=InstallButton.IsEnabled=true;Installation.TrySetResult(succeeded);if(!succeeded)status.BringIntoView();else InstallButton.Focus();}
        };
    }
}
