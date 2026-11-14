using System.Windows;
using System.Windows.Controls;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    Func<string?>? subscriptionClipboardReadFixture33;
    Action<string>? subscriptionClipboardWriteFixture33;

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
        var copy=Button("COPY",()=>{
            if(!Editable()||string.IsNullOrWhiteSpace(input.Text))return;
            try{
                if(fixtureMode&&subscriptionClipboardWriteFixture33!=null)subscriptionClipboardWriteFixture33(input.Text);else Clipboard.SetText(input.Text);
                Status("COPIED");
            }catch{Status("CLIPBOARD_UNAVAILABLE",true);}
        });copy.Tag="SUBSCRIPTION_COPY";
        foreach(var button in new[]{paste,copy}){button.MinHeight=44;button.MinWidth=100;button.FontSize=14;button.Padding=new(12,6,12,6);actions.Children.Add(button);}
        copy.IsEnabled=!string.IsNullOrWhiteSpace(input.Text);
        input.TextChanged+=(_,_)=>{copy.IsEnabled=!string.IsNullOrWhiteSpace(input.Text);feedback.Text="";};
        body.Children.Add(actions);body.Children.Add(feedback);return body;
    }
}
