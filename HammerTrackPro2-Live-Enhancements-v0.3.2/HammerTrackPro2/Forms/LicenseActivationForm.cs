using HammerTrackPro2.Services;

namespace HammerTrackPro2.Forms;

public sealed class LicenseActivationForm : Form
{
    private readonly Label _status=new() { AutoSize=true,MaximumSize=new Size(520,0) };
    public LicenseActivationForm(string reason)
    {
        Text="Activate HammerTrack Pro"; StartPosition=FormStartPosition.CenterScreen; Width=620; Height=300; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
        var machineId=LicenseService.GetMachineId(); var machine=new TextBox { Text=machineId,ReadOnly=true,Width=300 };
        var import=new Button { Text="Import License",AutoSize=true }; import.Click+=(_,_)=>ImportLicense();
        var copy=new Button { Text="Copy Machine ID",AutoSize=true }; copy.Click+=(_,_)=>Clipboard.SetText(machineId);
        var cancel=new Button { Text="Exit",AutoSize=true,DialogResult=DialogResult.Cancel };
        var layout=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(20) };
        layout.Controls.Add(new Label { Text="HammerTrack Pro requires a signed license for this PC.",AutoSize=true,Font=new Font("Segoe UI",12,FontStyle.Bold) }); layout.Controls.Add(new Label { Text="Send this Machine ID to your HammerTrack Pro license issuer:",AutoSize=true });
        var row=new FlowLayoutPanel { AutoSize=true }; row.Controls.Add(machine); row.Controls.Add(copy); layout.Controls.Add(row); _status.Text=reason; layout.Controls.Add(_status);
        var buttons=new FlowLayoutPanel { AutoSize=true }; buttons.Controls.Add(import); buttons.Controls.Add(cancel); layout.Controls.Add(buttons); Controls.Add(layout); AcceptButton=import; CancelButton=cancel;
    }

    private void ImportLicense()
    {
        using var dialog=new OpenFileDialog { Filter="HammerTrack Pro license (*.htplicense)|*.htplicense|All files (*.*)|*.*",CheckFileExists=true };
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try { var license=LicenseService.InstallLicense(dialog.FileName); MessageBox.Show($"Licensed to {license.CustomerName}.","License Installed",MessageBoxButtons.OK,MessageBoxIcon.Information); DialogResult=DialogResult.OK; Close(); }
        catch(Exception exception) { _status.Text=exception.Message; _status.ForeColor=Color.DarkRed; }
    }
}
