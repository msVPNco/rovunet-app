using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    ContextMenu HtmlMenu29(ContextMenu menu){menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent));return menu;}
    object[] HtmlMenuItems29(ItemsControl items)=>items.Items.Cast<object>().Select(item=>item switch{
        Separator=>new{kind="separator"} as object,
        MenuItem menu=>new{kind="item",id=HtmlId29(menu),label=menu.Header?.ToString()??"",enabled=menu.IsEnabled,selected=menu.IsChecked,children=HtmlMenuItems29(menu)},
        _=>new{kind="skip"}
    }).ToArray();
    object HtmlModel29()
    {
        var strings=new[]{"CONNECTED","DISCONNECTED","CONNECT","DISCONNECT","CONNECTING","CANCEL","STOP_TEST","HOME","QUICK_PING","DETAILED_HTTPS","SEARCH","MORE","EDIT","DELETE","ADD","SETTINGS","RESULT_STALE","PING_SCOPE","HIDE_PATHS","SHOW_PATHS"}.ToDictionary(key=>key,L.Text);
        return new{
            version=1,theme=SystemParameters.HighContrast?"contrast":darkTheme?IsAmoled?"amoled":"dark":"light",language=L.Language,rtl=L.Direction==FlowDirection.RightToLeft,
            // The HTML shell paints itself from CSS variables, so the chosen accent has
            // to travel with the model; theme changes alone would leave it at the default.
            accent=HtmlAccent27(),
            focused=IsActive&&WindowState!=WindowState.Minimized,motion=data.MarqueeMotion!="off"&&(data.MarqueeMotion=="on"||SystemParameters.ClientAreaAnimation)&&!SystemParameters.HighContrast,
            compact=compactLayout,busy,strings,depth=NavigationDepth,pageKey=NavigationDepth==0?"home":HtmlId29(pageStack[^1].PageVisual).ToString(),
            header=new{leading=HtmlNode29(header21.Leading),trailing=HtmlNode29(header21.Trailing)},
            // Language flags are gone from the view; the three-dot menu offers them.
            flags=Array.Empty<object>(),
            navigation=sidebarRows20.Select(entry=>new{key=entry.Key,label=L.Text(entry.Key),id=HtmlId29(entry.Value),selected=sidebarRoute20==entry.Key,path=MenuIcon(entry.Key)}),
            hero=new{state=state.State,title=heroTitle.Text,name=heroName21.Text,detail=heroDetail.Text,ping=heroPing.Text,color=HtmlBrush29(heroTitle.Foreground),detailColor=HtmlBrush29(heroDetail.Foreground),enabled=connect.IsEnabled},
            query=search.Text,searchOpen=search.Visibility==Visibility.Visible,searchAction=AutomationProperties.GetItemStatus(homeSearchAction30),
            groups=cards.Children.Cast<UIElement>().Select(tile=>groups.FirstOrDefault(group=>ReferenceEquals(group.Card,tile))).Where(group=>group!=null&&group.Card.Visibility==Visibility.Visible).Select(g=>new{
                id=g!.Key,name=g.Members[0].CollectionName.Length>0?g.Members[0].CollectionName:g.Members[0].Name,
                meta=L.Text(g.Members[0].SourceId.Length>0?"COLLECTION_SUBSCRIPTION":"COLLECTION_MANUAL")+" · "+string.Format(L.Text("PATH_COUNT"),g.Members.Count),
                open=data.OpenCollectionId==g.Key,active=state.State=="connected"&&g.Members.Any(p=>p.Id==activeProfile?.Id),selected=g.Members.Any(p=>p.Id==data.SelectedId),powerEnabled=g.Connect.IsEnabled,powerLabel=AutomationProperties.GetName(g.Connect),
                status=g.ClosedLine26?.Visibility==Visibility.Visible?g.ClosedStatus26?.Text:"",summary=g.Summary.Visibility==Visibility.Visible?g.Summary.Text:"",
                activity=CardActivity30(g) is {} work?new{text=work.Text,running=work.Running,color=HtmlBrush29(work.Ink)}:null,
                quota=g.Quota?.Root.Visibility==Visibility.Visible?new{used=g.Quota.Used.Text,remaining=g.Quota.Remaining.Text,time=g.Quota.Time.Text,note=g.Quota.Note.Visibility==Visibility.Visible?g.Quota.Note.Text:"",percent=g.Quota.Percent.Text}:null,
                running=collectionRuns.GetValueOrDefault(g.Key)?.Running==true,stopping=testStop?.IsCancellationRequested==true,progress=g.Progress.Value,total=g.Progress.Maximum,testEnabled=g.Test.IsEnabled,
                rows=data.OpenCollectionId==g.Key?g.Paths.Children.OfType<Border>().Select(tile=>rows.FirstOrDefault(r=>ReferenceEquals(r.Tile,tile))).Where(r=>r!=null&&r.Tile.Visibility==Visibility.Visible).Select(r=>new{
                    id=r!.Profile.Id,name=r.Profile.Name,host=ProfileHost(r.Profile),protocol=r.Protocol.Text,result=r.Result.Visibility==Visibility.Visible?r.Result.Text:"",resultColor=HtmlBrush29(r.Result.Foreground),ping=r.Timing.Visibility==Visibility.Visible?r.Timing.Text:"",pingColor=HtmlBrush29(r.Timing.Foreground),selected=r.Profile.Id==data.SelectedId,active=state.State=="connected"&&r.Profile.Id==activeProfile?.Id,edit=r.Edit.IsEnabled,delete=r.Delete.IsEnabled,speed=SpeedTier27.Color(r.Profile)
                }).Cast<object>().ToArray():[]
            }),
            page=NavigationDepth==0?null:HtmlNode29(pageStack[^1].PageVisual),
            footer=new{title=connectionText.Text,detail=detail.Text,color=HtmlBrush29(connectionText.Foreground),up=trafficUp.Text,down=trafficDown.Text,total=trafficTotal.Text,notice=notice.Visibility==Visibility.Visible?notice.Text:"",noticeColor=HtmlBrush29(notice.Foreground),error=errorText.Visibility==Visibility.Visible?errorText.Text:""},
            popup=htmlPopup29==null?null:HtmlMenuItems29(htmlPopup29),emptyTitle=L.Text("EMPTY_TITLE"),emptyHint=L.Text("ADD_HUB_HINT")
        };
    }
    object HtmlAccent27()
    {
        var accent=Accent27(data.AccentColor);
        // Tones derive from the single accent colour so every theme gets the same
        // family: a soft container for chips/rows and a ring for the power button.
        return new{
            value=accent.Value,
            light=accent.Light,
            dark=accent.Dark,
            groupLight=Mix27(accent.Light,"#EDF1F6",0.30),
            groupDark=Mix27(accent.Dark,"#252F3E",0.32),
            softLight=Mix27(accent.Light,"#F5F5F7",0.14),
            softDark=Mix27(accent.Dark,"#15171D",0.18),
            ringLight=Mix27(accent.Light,"#FFFFFF",0.45),
            ringDark=Mix27(accent.Dark,"#000000",0.30)
        };
    }
    static string Mix27(string accent,string baseColor,double share)
    {
        var a=(Color)ColorConverter.ConvertFromString(accent);
        var b=(Color)ColorConverter.ConvertFromString(baseColor);
        byte Mix(byte x,byte y)=> (byte)Math.Round(x*share+y*(1-share));
        return $"#{Mix(a.R,b.R):x2}{Mix(a.G,b.G):x2}{Mix(a.B,b.B):x2}";
    }
    // A swatch button shows the colour it would apply, so the picker is usable
    // without reading eight colour names.
    static string AccentSwatch29(string value,bool dark)
    {
        var accent=Accent27(value);
        return dark?accent.Dark:accent.Light;
    }
    static string HtmlBrush29(Brush? brush)=>brush switch{
        SolidColorBrush solid=>$"#{solid.Color.R:x2}{solid.Color.G:x2}{solid.Color.B:x2}",
        LinearGradientBrush gradient=>"linear-gradient(135deg,"+string.Join(',',gradient.GradientStops.Select(stop=>$"#{stop.Color.R:x2}{stop.Color.G:x2}{stop.Color.B:x2} {stop.Offset*100:0.##}%"))+")",
        _=>"inherit"
    };
    readonly Dictionary<ImageSource,string> htmlImages29=new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    readonly Dictionary<long,int> htmlScanOffsets29=[];
    readonly Dictionary<string,string> htmlFlags29=[];
    string HtmlFlag29(string code){if(!htmlFlags29.TryGetValue(code,out var image))htmlFlags29[code]=image=HtmlImage29(FlagImage(code).Source!);return image;}
    string HtmlImage29(ImageSource image)
    {
        if(htmlImages29.TryGetValue(image,out var encoded))return encoded;
        int width=image is BitmapSource bitmap?Math.Min(512,bitmap.PixelWidth):image.Width>0?Math.Min(128,(int)image.Width):64;
        int height=image.Width>0&&image.Height>0?Math.Max(1,(int)Math.Round(width*image.Height/image.Width)):width;
        var drawing=new DrawingVisual();using(var context=drawing.RenderOpen())context.DrawImage(image,new Rect(0,0,width,height));
        var target=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);target.Render(drawing);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(target));using var stream=new MemoryStream();png.Save(stream);
        encoded="data:image/png;base64,"+Convert.ToBase64String(stream.ToArray());if(htmlImages29.Count>128)htmlImages29.Clear();htmlImages29[image]=encoded;return encoded;
    }
    object? HtmlNode29(object? source,int depth=0)
    {
        if(depth>30||source==null)return null;
        if(source is string value)return new{kind="text",text=value};
        if(source is not FrameworkElement element||element.Visibility!=Visibility.Visible)return null;
        if(AutomationProperties.GetName(element)=="Rovunet")return new{kind="brand"};
        var node=new Dictionary<string,object?>{["kind"]="stack",["children"]=Array.Empty<object>(),["row"]=Grid.GetRow(element),["column"]=Grid.GetColumn(element),["rowspan"]=Grid.GetRowSpan(element),["colspan"]=Grid.GetColumnSpan(element)};
        node["direction"]=element.FlowDirection==FlowDirection.RightToLeft?"rtl":"ltr";
        if(double.IsFinite(element.Width))node["width"]=element.Width;
        if(double.IsFinite(element.Height))node["height"]=element.Height;
        object? Child(object? child)=>HtmlNode29(child,depth+1);
        object[] Children(IEnumerable children)=>children.Cast<object>().Select(Child).Where(child=>child!=null).Cast<object>().ToArray();
        switch(element){
            case MarqueeLine line:node["kind"]="marquee";node["children"]=new[]{Child(line.ContentSource29)};break;
            case TextBlock text:node["kind"]="text";node["text"]=text.Text;node["color"]=HtmlBrush29(text.Foreground);node["size"]=text.FontSize;node["weight"]=text.FontWeight.ToOpenTypeWeight();node["nowrap"]=text.TextWrapping==TextWrapping.NoWrap;break;
            case PasswordBox password:node["kind"]="password";node["id"]=HtmlId29(password);node["value"]=password.Password;node["enabled"]=password.IsEnabled;break;
            case TextBox input:node["kind"]="input";node["id"]=HtmlId29(input);node["value"]=input.Text;node["enabled"]=input.IsEnabled;node["readonly"]=input.IsReadOnly;node["multiline"]=input.AcceptsReturn;break;
            case ComboBox combo:node["kind"]="select";node["id"]=HtmlId29(combo);node["enabled"]=combo.IsEnabled;node["value"]=combo.SelectedIndex;node["options"]=combo.Items.Cast<object>().Select(item=>L.CurrentLiteral(item is ComboBoxItem option?option.Content?.ToString()??"":combo.DisplayMemberPath.Length>0?item.GetType().GetProperty(combo.DisplayMemberPath)?.GetValue(item)?.ToString()??item.ToString()??"":item.ToString()??"")).ToArray();break;
            case TabControl tabs:node["kind"]="tabs";node["id"]=HtmlId29(tabs);node["enabled"]=tabs.IsEnabled;node["value"]=tabs.SelectedIndex;node["options"]=tabs.Items.OfType<TabItem>().Select(tab=>tab.Header?.ToString()??"").ToArray();node["children"]=new[]{Child((tabs.SelectedItem as TabItem)?.Content)};break;
            case ListBox list when list.Items.Count==0||list.Items[0] is ScanEntry:
                long listId=HtmlId29(list);int offset=Math.Clamp(htmlScanOffsets29.GetValueOrDefault(listId),0,Math.Max(0,list.Items.Count-1));node["kind"]="scan-list";node["id"]=listId;node["total"]=list.Items.Count;node["offset"]=offset;
                node["rows"]=list.Items.OfType<ScanEntry>().Skip(offset).Take(150).Select(entry=>new{address=entry.Address,transport=entry.TransportLabel,ping=entry.DnsPing,checks=entry.ChecksLabel,description=entry.Description,color=HtmlBrush29(entry.ResultBrush),pingColor=HtmlBrush29(entry.DnsBrush),enabled=entry.Selectable&&!entry.Stale,selected=list.SelectedItems.Contains(entry)}).ToArray();break;
            case CheckBox check:node["kind"]="check";node["id"]=HtmlId29(check);node["enabled"]=check.IsEnabled;node["checked"]=check.IsChecked==true;node["children"]=new[]{Child(check.Content)};break;
            case RadioButton radio:node["kind"]="radio";node["id"]=HtmlId29(radio);node["enabled"]=radio.IsEnabled;node["checked"]=radio.IsChecked==true;node["children"]=new[]{Child(radio.Content)};break;
            case Button button:node["kind"]="button";node["id"]=HtmlId29(button);node["enabled"]=button.IsEnabled;node["danger"]=button.Uid is "STOP_TEST" or "SCAN_STOP_TUNNEL";node["label"]=AutomationProperties.GetName(button) is {Length:>0} name?name:button.ToolTip?.ToString()??button.Content as string??"";node["swatch"]=button.Tag is string accentTag&&accentTag.StartsWith("accent:",StringComparison.Ordinal)?AccentSwatch29(accentTag[7..],darkTheme):null;node["selected"]=button.Tag is string currentTag&&currentTag.StartsWith("accent:",StringComparison.Ordinal)&&currentTag[7..]==data.AccentColor;node["children"]=new[]{Child(button.Content)};if(button.Tag is Geometry glyph)node["icon"]=glyph.ToString(System.Globalization.CultureInfo.InvariantCulture);break;
            case Expander expander:node["kind"]="expander";node["id"]=HtmlId29(expander);node["enabled"]=expander.IsEnabled;node["label"]=expander.Header?.ToString()??"";node["checked"]=expander.IsExpanded;node["children"]=expander.IsExpanded?new[]{Child(expander.Content)}:[];break;
            case Slider slider:node["kind"]="slider";node["id"]=HtmlId29(slider);node["enabled"]=slider.IsEnabled;node["min"]=slider.Minimum;node["max"]=slider.Maximum;node["value"]=slider.Value;break;
            case ProgressBar progress:node["kind"]="progress";node["max"]=Math.Max(1,progress.Maximum);node["value"]=progress.Value;break;
            case Image image when image.Source!=null:node["kind"]="image";node["image"]=HtmlImage29(image.Source);node["width"]=double.IsFinite(image.Width)?image.Width:160;break;
            case System.Windows.Shapes.Path path:node["kind"]="icon";node["path"]=path.Data is {} dataGeometry?PathGeometry.CreateFromGeometry(dataGeometry).ToString(System.Globalization.CultureInfo.InvariantCulture):"";node["filled"]=path.Fill!=null;node["color"]=HtmlBrush29(path.Fill??path.Stroke);node["viewbox"]=path.Data is {} geometry?FormattableString.Invariant($"{geometry.Bounds.X} {geometry.Bounds.Y} {Math.Max(1,geometry.Bounds.Width)} {Math.Max(1,geometry.Bounds.Height)}"):"0 0 24 24";break;
            case Viewbox view:node["children"]=new[]{Child(view.Child)};break;
            case Border border:node["kind"]=border.Background!=null&&border.Background!=Brushes.Transparent?"surface":"stack";node["children"]=new[]{Child(border.Child)};break;
            case ScrollViewer scroll:node["children"]=new[]{Child(scroll.Content)};break;
            case ContentControl content:node["children"]=new[]{Child(content.Content)};break;
            case Grid grid:
                var label=grid.Children.OfType<TextBlock>().LastOrDefault();var frame=grid.Children.OfType<Border>().FirstOrDefault();
                if(label!=null&&frame?.Child is Control){node["kind"]="field";node["label"]=label.Text;node["children"]=new[]{Child(frame.Child)};}
                else{node["kind"]="grid";node["columns"]=grid.ColumnDefinitions.Count==0?"1fr":string.Join(' ',grid.ColumnDefinitions.Select(column=>column.Width.IsAuto?"max-content":column.Width.IsStar?"minmax(0,"+column.Width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)+"fr)":column.Width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)+"px"));node["children"]=Children(grid.Children);}break;
            case UniformGrid uniform:
                var cells=Children(uniform.Children);int columns=uniform.Columns>0?uniform.Columns:uniform.Rows>0?(int)Math.Ceiling((double)cells.Length/uniform.Rows):(int)Math.Ceiling(Math.Sqrt(cells.Length));columns=Math.Max(1,columns);
                int firstColumn=uniform.FirstColumn<columns?uniform.FirstColumn:0;
                for(int i=0;i<cells.Length;i++)if(cells[i] is Dictionary<string,object?> cell){cell["row"]=(i+firstColumn)/columns;cell["column"]=(i+firstColumn)%columns;}
                node["kind"]="grid";node["uniform"]=true;node["columns"]=$"repeat({columns},minmax(0,1fr))";node["children"]=cells;break;
            case StackPanel stack:node["kind"]=stack.Orientation==Orientation.Horizontal?"row":"stack";node["children"]=Children(stack.Children);break;
            case WrapPanel wrap:node["kind"]="wrap";node["children"]=Children(wrap.Children);break;
            case DockPanel dock when Equals(dock.Tag,"PAGE_TOOLBAR30"):node["kind"]="toolbar";node["children"]=Children(dock.Children);break;
            case Panel panel:node["children"]=Children(panel.Children);break;
            case ItemsControl items:node["children"]=Children(items.Items);break;
            default:return null;
        }
        return node;
    }
}
