using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class HandheldLicenseScannerForm : Form
{
    private static readonly Color Navy=Color.FromArgb(24,50,74),Blue=Color.FromArgb(24,96,150),Gold=Color.FromArgb(224,168,54);
    private readonly HandheldLicenseScanViewModel _viewModel=new();
    private readonly Label _status=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",12F,FontStyle.Bold),ForeColor=Navy};
    private readonly System.Windows.Forms.Timer _completionTimer=new(){Interval=550};
    public DriverLicenseScanData? Result=>_viewModel.Result;

    public HandheldLicenseScannerForm()
    {
        Text="HammerTrack Pro — Handheld License Scanner";Width=720;Height=410;MinimumSize=new Size(620,360);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(242,245,248);KeyPreview=true;
        var header=new Label{Text="HANDHELD DRIVER'S LICENSE SCANNER",Dock=DockStyle.Top,Height=72,Padding=new Padding(20,0,0,0),TextAlign=ContentAlignment.MiddleLeft,BackColor=Navy,ForeColor=Color.White,Font=new Font("Segoe UI",18F,FontStyle.Bold)};
        var instructions=new Label{Text="Connect the NETUM scanner in USB HID / keyboard mode. Make sure PDF417 is enabled, then pull the scanner trigger. You do not need to click a text box.",Dock=DockStyle.Top,Height=88,Padding=new Padding(24,16,24,8),TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.White,ForeColor=Navy,Font=new Font("Segoe UI",11F)};
        var privacy=new Label{Text="The raw barcode is hidden and discarded after parsing. Review the filled bidder fields before saving.",Dock=DockStyle.Bottom,Height=45,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.DimGray};
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=62,FlowDirection=FlowDirection.LeftToRight,Padding=new Padding(155,7,0,7),BackColor=Color.White};actions.Controls.Add(Button("Reset / Scan Again",Reset,true));actions.Controls.Add(Button("Cancel",Close));
        Controls.Add(_status);Controls.Add(privacy);Controls.Add(actions);Controls.Add(instructions);Controls.Add(header);
        _status.Text=_viewModel.Status;KeyPress+=OnScannerCharacter;_completionTimer.Tick+=(_,_)=>CompleteScan();FormClosed+=(_,_)=>_completionTimer.Dispose();Shown+=(_,_)=>Activate();Activated+=(_,_)=>_status.Text=_viewModel.CharacterCount==0?_viewModel.Status:$"Receiving license barcode... {_viewModel.CharacterCount} characters";
    }

    protected override bool ProcessCmdKey(ref Message message,Keys keyData)
    {
        if((keyData&Keys.KeyCode)==Keys.Tab){ReceiveCharacter('\t');return true;}
        return base.ProcessCmdKey(ref message,keyData);
    }

    private void OnScannerCharacter(object? sender,KeyPressEventArgs eventArgs){eventArgs.Handled=true;ReceiveCharacter(eventArgs.KeyChar);}
    private void ReceiveCharacter(char character){_viewModel.Receive(character);_status.Text=_viewModel.Status;_status.ForeColor=Navy;_completionTimer.Stop();_completionTimer.Start();}
    private void CompleteScan(){_completionTimer.Stop();if(_viewModel.Complete()){System.Media.SystemSounds.Asterisk.Play();DialogResult=DialogResult.OK;Close();return;}_status.Text=_viewModel.Status;_status.ForeColor=Color.DarkRed;}
    private void Reset(){_completionTimer.Stop();_viewModel.Reset();_status.Text=_viewModel.Status;_status.ForeColor=Navy;Activate();}
    private static Button Button(string text,Action action,bool accent=false){var button=new Button{Text=text,Width=180,Height=44,Margin=new Padding(6),FlatStyle=FlatStyle.Flat,BackColor=accent?Gold:Blue,ForeColor=accent?Navy:Color.White,Font=new Font("Segoe UI",10F,FontStyle.Bold),Cursor=Cursors.Hand};button.FlatAppearance.BorderColor=accent?Gold:Navy;button.Click+=(_,_)=>action();return button;}
}
