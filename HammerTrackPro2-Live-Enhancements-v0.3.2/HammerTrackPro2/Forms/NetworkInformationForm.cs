using HammerTrackPro2.Services;

namespace HammerTrackPro2.Forms;

public sealed class NetworkInformationForm : Form
{
    public NetworkInformationForm(WorkstationConfiguration configuration)
    {
        Text="Network Information"; StartPosition=FormStartPosition.CenterParent; Width=720; Height=330; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
        var url=$"https://{Environment.MachineName}:{configuration.ServicePort}";
        var address=System.Net.Dns.GetHostAddresses(Environment.MachineName).FirstOrDefault(item=>item.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&!System.Net.IPAddress.IsLoopback(item));
        var alternate=address is null?"Not detected":$"https://{address}:{configuration.ServicePort}";
        var form=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(20) }; form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var heading=new Label { Text="Cashier pairing information",Font=new Font("Segoe UI",16,FontStyle.Bold),AutoSize=true }; form.Controls.Add(heading,0,0); form.SetColumnSpan(heading,2);
        Add(form,"Host address",url,1); Add(form,"Alternate IP address",alternate,2); Add(form,"Pairing key",configuration.PairingKey,3); Add(form,"Certificate fingerprint",configuration.CertificateThumbprint,4);
        var note=new Label { Text="Keep the pairing key private. Enter these values once on the cashier PC. Both computers must stay on the same private network.",AutoSize=true,MaximumSize=new Size(500,70) }; form.Controls.Add(note,1,5); Controls.Add(form);
        var setup=new Button { Text="Change Workstation Setup",AutoSize=true }; setup.Click+=(_,_)=>{ using var dialog=new WorkstationSetupForm(configuration); if(dialog.ShowDialog(this)==DialogResult.OK) MessageBox.Show("Restart HammerTrack to apply the network change.","Network Setup"); }; form.Controls.Add(setup,1,6);
    }
    private static void Add(TableLayoutPanel form,string label,string value,int row) { form.Controls.Add(new Label { Text=label,AutoSize=true,Anchor=AnchorStyles.Left },0,row); form.Controls.Add(new TextBox { Text=value,ReadOnly=true,Dock=DockStyle.Fill },1,row); }
}
