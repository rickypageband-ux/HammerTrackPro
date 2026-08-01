using HammerTrackPro2.Services;

namespace HammerTrackPro2.Forms;

public sealed class WorkstationSetupForm : Form
{
    private readonly RadioButton _host=new() { Text="Live Auction Host (stores the database)",AutoSize=true,Checked=true };
    private readonly RadioButton _cashier=new() { Text="Cashier Workstation (connects to host)",AutoSize=true };
    private readonly TextBox _hostUrl=new() { PlaceholderText="https://AUCTION-PC:7443" };
    private readonly TextBox _pairingKey=new(); private readonly TextBox _fingerprint=new();
    public WorkstationConfiguration Configuration { get; private set; }
    public WorkstationSetupForm(WorkstationConfiguration configuration)
    {
        Configuration=configuration; Text="HammerTrack Network Setup"; StartPosition=FormStartPosition.CenterScreen; Width=650; Height=390; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
        _host.Checked=configuration.Role==WorkstationRole.Host; _cashier.Checked=configuration.Role==WorkstationRole.Cashier;
        _hostUrl.Text=configuration.HostUrl; _pairingKey.Text=configuration.PairingKey; _fingerprint.Text=configuration.CertificateThumbprint;
        var form=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2,AutoSize=true,Padding=new Padding(20) }; form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var heading=new Label { Text="Choose this computer's role",Font=new Font("Segoe UI",16,FontStyle.Bold),AutoSize=true };
        form.Controls.Add(heading,0,0); form.SetColumnSpan(heading,2); form.Controls.Add(_host,1,1); form.Controls.Add(_cashier,1,2);
        Add(form,"Host address",_hostUrl,3); Add(form,"Pairing key",_pairingKey,4); Add(form,"Certificate fingerprint",_fingerprint,5);
        var help=new Label { Text="On the cashier PC, copy these three values from Network Information on the live-auction PC.",AutoSize=true,MaximumSize=new Size(520,50) };
        form.Controls.Add(help,1,6); var save=new Button { Text="Save and Continue",AutoSize=true,Padding=new Padding(12,5,12,5) }; save.Click+=(_,_)=>Save(); form.Controls.Add(save,1,7); Controls.Add(form);
        _host.CheckedChanged+=(_,_)=>UpdateEnabled(); UpdateEnabled();
    }
    private static void Add(TableLayoutPanel form,string label,Control control,int row) { control.Dock=DockStyle.Fill; form.Controls.Add(new Label { Text=label,AutoSize=true,Anchor=AnchorStyles.Left },0,row); form.Controls.Add(control,1,row); }
    private void UpdateEnabled() { var remote=_cashier.Checked; _hostUrl.Enabled=remote; _pairingKey.Enabled=remote; _fingerprint.Enabled=remote; }
    private void Save()
    {
        if(_cashier.Checked && (!Uri.TryCreate(_hostUrl.Text.Trim(),UriKind.Absolute,out var url)||url.Scheme!="https"||string.IsNullOrWhiteSpace(_pairingKey.Text)||string.IsNullOrWhiteSpace(_fingerprint.Text)))
        { MessageBox.Show("Enter the HTTPS host address, pairing key, and certificate fingerprint shown on the live-auction PC.","Network Setup",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; }
        Configuration.Role=_cashier.Checked?WorkstationRole.Cashier:WorkstationRole.Host; Configuration.HostUrl=_hostUrl.Text.Trim();
        if(_cashier.Checked) { Configuration.PairingKey=_pairingKey.Text.Trim(); Configuration.CertificateThumbprint=_fingerprint.Text.Trim(); }
        WorkstationConfigurationService.Save(Configuration); DialogResult=DialogResult.OK; Close();
    }
}
