using System.Windows;
using System.Windows.Controls;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    Func<string?>? subscriptionClipboardReadFixture33;

    FrameworkElement SubscriptionClipboardActions33(TextBox input,FrameworkElement inputFrame)
    {
        var body=new StackPanel{Margin=new(0,0,0,8),Tag="SUBSCRIPTION_CLIPBOARD"};
        var actions=new WrapPanel();var feedback=Text("",12,Muted);feedback.Tag="SUBSCRIPTION_CLIPBOARD_FEEDBACK";
        bool Editable()=>input.IsEnabled&&!input.IsReadOnly&&inputFrame.Visibility==Visibility.Visible&&body.IsVisible;
        void Status(string key,bool failed=false){feedback.Text=L.Text(key);feedback.Foreground=failed?Red:Green;QueueHtml29();}
        var paste=Button("PASTE",()=>{
            if(!Editable())return;
            try{
                string value=(fixtureMode&&subscriptionClipboardReadFixture33!=null?subscriptionClipboardReadFixture33():Clipboard.ContainsText()?Clipboard.GetText():"")??"";
                if(string.IsNullOrWhiteSpace(value)){Status("CLIPBOARD_EMPTY",true);return;}
                if(value.Length>65536){Status("IMPORT_TOO_LARGE",true);return;}
                input.Text=value.Trim();input.CaretIndex=input.Text.Length;Status("PASTED");
            }catch{Status("CLIPBOARD_UNAVAILABLE",true);}
        });paste.Tag="SUBSCRIPTION_PASTE";
        paste.MinHeight=44;paste.MinWidth=100;paste.FontSize=14;paste.Padding=new(12,6,12,6);actions.Children.Add(paste);
        input.TextChanged+=(_,_)=>feedback.Text="";
        body.Children.Add(actions);body.Children.Add(feedback);return body;
    }
}
