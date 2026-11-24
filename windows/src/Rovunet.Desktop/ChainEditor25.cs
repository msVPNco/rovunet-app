using System.Windows;
using System.Windows.Controls;
using Rovunet.Core;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    List<ChainIdentity> ChainIdentityOptions25(Profile p)=>!ChainProfiles.IsChain(p.Raw)?[]:ChainProfiles.Read(p.Raw).Layers.Select(layer=>new ChainIdentity{HostKey=StoredSshKey(layer),SlipCertificate=StoredSlipPin(layer),SshTlsCa=StoredSshTlsCa(layer),SlipInterval=SlipInterval(layer)}).ToList();
    Profile TrustLayer25(Profile p,AgentReply reply)=>ChainProfiles.IsChain(p.Raw)&&reply.ChainLayerIndex>=0?ChainProfiles.Read(p.Raw).Layers[reply.ChainLayerIndex]:p;

    // Stepwise builder: each row lists only profiles that can legally sit at that
    // position given the rows above it, so an impossible chain cannot be assembled
    // and the user never meets a save-time rejection after a long selection.
    DialogWindow BuildChainEditor25(Profile? original=null)
    {
        var page=Dialog("CHAIN_TITLE");
        var panel=new StackPanel{Margin=new(20)};
        var name=new TextBox{Text=original?.Name??L.Text("CHAIN_TITLE"),Tag="chain:name"};
        panel.Children.Add(OutlinedField("PROFILE_NAME",name));
        panel.Children.Add(Text(L.Text("CHAIN_HINT"),13,Muted));
        panel.Children.Add(Text(L.Text("CHAIN_FILTERED_HINT"),13,Muted));

        var pool=data.Profiles.Where(p=>!ChainProfiles.IsChain(p.Raw)&&p.CanConnect).GroupBy(p=>p.Id).Select(g=>g.First()).ToList();
        var current=original==null?new List<Profile>():ChainProfiles.Read(original.Raw).Layers;
        var selects=new List<ComboBox>();
        var rows=new StackPanel();
        var preview=Text("",14);      // "VLESS WS → SSH" as it is assembled
        var validity=Text("",13,Red); // why the current chain cannot be saved, or empty
        var noFit=Text("",13,Muted);  // a position no saved profile can legally fill
        bool refreshing=false;
        Button? add=null;
        panel.Children.Add(rows);
        panel.Children.Add(noFit);
        panel.Children.Add(preview);
        panel.Children.Add(validity);

        List<Profile> Prefix(int count)
        {
            var list=new List<Profile>(count);
            for(int i=0;i<count&&i<selects.Count;i++)if(selects[i].SelectedItem is Profile p)list.Add(p);
            return list;
        }
        void RefreshChoices()
        {
            if(refreshing)return;
            refreshing=true;
            try{
                for(int i=0;i<selects.Count;i++){
                    var box=selects[i];
                    var keep=box.SelectedItem as Profile;
                    var allowed=new List<Profile>(ChainProfiles.Appendable(Prefix(i),pool));
                    if(keep!=null&&!allowed.Contains(keep))allowed.Add(keep); // an already-chosen layer stays visible
                    box.ItemsSource=allowed;
                    box.DisplayMemberPath="Name";
                    box.SelectedItem=keep;
                    box.IsEnabled=allowed.Count>0;
                }
            }finally{refreshing=false;}
            var last=selects.Count==0?null:selects[selects.Count-1];
            noFit.Text=last!=null&&last.Items.Count==0?L.Text("CHAIN_NO_LAYER"):"";
            ShowPath();
            UpdateAdd();
        }
        void ShowPath()
        {
            var chosen=Prefix(selects.Count);
            preview.Text=chosen.Count==0?"":string.Join(" → ",chosen.Select(p=>p.Protocol.Length>0?p.Protocol:ChainProfiles.Kind(p)));
            string? problem=chosen.Count==0?null:ChainProfiles.Rule(chosen,complete:true);
            // An incomplete chain is not an error yet: the user is still choosing.
            validity.Text=chosen.Count>=2&&problem!=null?L.Text(problem):"";
        }
        void UpdateAdd()
        {
            if(add==null)return;
            add.IsEnabled=selects.Count<4&&Prefix(selects.Count).Count==selects.Count&&ChainProfiles.Appendable(Prefix(selects.Count),pool).Count>0;
        }
        void AddLayer(Profile? selected=null)
        {
            if(selects.Count>=4)return;
            var box=new ComboBox{Tag="chain:layer:"+selects.Count,MinHeight=44,DisplayMemberPath="Name"};
            box.SelectionChanged+=(_,_)=>{if(!refreshing)RefreshChoices();};
            selects.Add(box);
            rows.Children.Add(OutlinedField("CHAIN_LAYER",box));
            box.ItemsSource=selected!=null?new List<Profile>(ChainProfiles.Appendable(Prefix(selects.Count-1),pool.Concat(new[]{selected}).Distinct())):new List<Profile>(ChainProfiles.Appendable(Prefix(selects.Count-1),pool));
            box.SelectedItem=selected;
            RefreshChoices();
            UpdateAdd();
            // A row added below the fold used to read as "the button did nothing";
            // bring the new layer into view so its dropdown is right there.
            box.Dispatcher.BeginInvoke(()=>box.BringIntoView());
        }

        if(current.Count>0)foreach(var layer in current)AddLayer(layer);
        else{AddLayer();AddLayer();}

        add=Button("CHAIN_ADD_LAYER",()=>AddLayer());
        var remove=Button("CHAIN_REMOVE_LAYER",()=>{
            if(selects.Count<=2)return;
            selects.RemoveAt(selects.Count-1);
            rows.Children.RemoveAt(rows.Children.Count-1);
            RefreshChoices();
        });
        var buttons=new WrapPanel();
        buttons.Children.Add(add);buttons.Children.Add(remove);
        panel.Children.Add(buttons);

        var error=Text("",13,Red);panel.Children.Add(error);
        void Apply()
        {
            try{
                var layers=selects.Select(b=>b.SelectedItem as Profile??throw new SafeError("SELECT_PATH")).ToList();
                string raw=ChainProfiles.Create(name.Text,layers);
                if(original==null)AddLocalProfile25(Importer.Single(raw));else SaveEdited(original,raw);
                page.DialogResult=true;
            }catch(Exception e){error.Text=L.Text(e is SafeError s?s.Code:"INVALID_IMPORT");}
        }
        page.SetPageAction(Button("SAVE",Apply,true));
        var save=Button(original==null?"ADD":"SAVE",Apply,true);
        // Save stays disabled until the assembled path is actually valid.
        void SyncSave(){bool ready=selects.Count>=2&&selects.Count==Prefix(selects.Count).Count&&ChainProfiles.Rule(Prefix(selects.Count),complete:true)==null;save.IsEnabled=ready;}
        selects.ForEach(box=>box.SelectionChanged+=(_,_)=>SyncSave());
        SyncSave();
        panel.Children.Add(save);
        page.Content=panel;
        RefreshChoices();
        UpdateAdd();
        return page;
    }
    void PrepareChainCertificates25(Profile p)
    {
        if(!ChainProfiles.IsChain(p.Raw))return;
        foreach(var layer in ChainProfiles.Read(p.Raw).Layers)if(TransportPlan.UsesSlip(layer.Raw)&&StoredSlipPin(layer).Length==0&&!ChooseSlipCertificate(layer))throw new SafeError("SLIP_CERT_REQUIRED");
    }
}
