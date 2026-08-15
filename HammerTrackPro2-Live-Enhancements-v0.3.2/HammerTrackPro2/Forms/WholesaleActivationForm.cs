using System.Diagnostics;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class WholesaleActivationForm : Form
{
    private readonly WholesaleActivationViewModel _viewModel;
    private readonly TextBox _key=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
    private readonly Label _status=new(){Dock=DockStyle.Fill,ForeColor=Color.DarkRed,AutoSize=true};

    public WholesaleActivationForm(WholesaleActivationViewModel viewModel,string reason)
    {
        _viewModel=viewModel;Text="Activate Wholesale Auction";StartPosition=FormStartPosition.CenterParent;MinimumSize=new Size(650,430);Width=720;Height=470;Font=new Font("Segoe UI",11F);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),RowCount=7,ColumnCount=1};
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label{Text="WHOLESALE AUCTION UPGRADE",AutoSize=true,Font=new Font("Segoe UI",20F,FontStyle.Bold),ForeColor=Color.FromArgb(24,50,74)},0,0);
        root.Controls.Add(new Label{Text="Paste the signed activation key below. This module is licensed separately from the main HammerTrack Pro license.",AutoSize=true,MaximumSize=new Size(640,0),Margin=new Padding(0,12,0,10)},0,1);
        root.Controls.Add(_key,0,2);
        var machineRow=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=3,Margin=new Padding(0,10,0,4)};
        machineRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));machineRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));machineRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        machineRow.Controls.Add(new Label{Text="Machine ID",AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(0,8,10,0)},0,0);
        var machineId=new TextBox{Name="WholesaleMachineId",Text=_viewModel.MachineId,ReadOnly=true,Dock=DockStyle.Fill,TabStop=false};
        var copyMachineId=new Button{Name="CopyWholesaleMachineId",Text="Copy Machine ID",AutoSize=true,MinimumSize=new Size(150,38),Margin=new Padding(10,0,0,0)};
        copyMachineId.Click+=(_,_)=>CopyMachineId();machineRow.Controls.Add(machineId,1,0);machineRow.Controls.Add(copyMachineId,2,0);root.Controls.Add(machineRow,0,3);
        _status.Text=reason;root.Controls.Add(_status,0,4);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,AutoSize=true,Margin=new Padding(0,14,0,0)};
        var activate=new Button{Text="Activate",AutoSize=true,MinimumSize=new Size(130,44),BackColor=Color.FromArgb(24,96,150),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
        var contact=new Button{Text="Contact Support",AutoSize=true,MinimumSize=new Size(150,44)};var cancel=new Button{Text="Cancel",AutoSize=true,MinimumSize=new Size(110,44)};
        activate.Click+=(_,_)=>ActivateModule();contact.Click+=(_,_)=>Process.Start(new ProcessStartInfo("https://hammertrackpro.com/contact"){UseShellExecute=true});cancel.Click+=(_,_)=>Close();buttons.Controls.AddRange([activate,contact,cancel]);root.Controls.Add(buttons,0,5);Controls.Add(root);AcceptButton=activate;CancelButton=cancel;
    }

    private void CopyMachineId()
    {
        try{Clipboard.SetText(_viewModel.MachineId);_status.ForeColor=Color.DarkGreen;_status.Text="Machine ID copied. Paste it into a message or the Wholesale license issuer.";}
        catch(Exception exception){_status.ForeColor=Color.DarkRed;_status.Text=$"The Machine ID could not be copied: {exception.Message}";}
    }

    private void ActivateModule()
    {
        try{_status.ForeColor=Color.DarkGreen;_status.Text=_viewModel.Activate(_key.Text);DialogResult=DialogResult.OK;Close();}
        catch(Exception exception){_status.ForeColor=Color.DarkRed;_status.Text=exception.Message;System.Media.SystemSounds.Hand.Play();}
    }
}
