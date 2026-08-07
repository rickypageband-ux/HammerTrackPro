using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class HandheldLicenseScannerForm : Form
{
    private static readonly Color Navy=Color.FromArgb(24,50,74),Blue=Color.FromArgb(24,96,150),Gold=Color.FromArgb(224,168,54);
    private readonly HandheldLicenseScanViewModel _viewModel=new();
    private readonly Label _status=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",12F,FontStyle.Bold),ForeColor=Navy};
    private readonly TextBox _scannerInput=new(){Multiline=true,AcceptsReturn=true,AcceptsTab=true,ShortcutsEnabled=true,TabStop=false,BorderStyle=BorderStyle.None,Size=new Size(1,1),Location=new Point(1,1)};
    private readonly System.Windows.Forms.Timer _completionTimer=new(){Interval=HandheldLicenseScanViewModel.InitialCompletionDelayMilliseconds};
    private bool _clearingScannerInput;
    private bool _waitingForSlowChunk;
    public DriverLicenseScanData? Result=>_viewModel.Result;

    public HandheldLicenseScannerForm(bool newPolycarbonateCard=false)
    {
        Text="HammerTrack Pro — Handheld License Scanner";Width=720;Height=410;MinimumSize=new Size(620,360);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(242,245,248);KeyPreview=true;
        var header=new Label{Text="HANDHELD DRIVER'S LICENSE SCANNER",Dock=DockStyle.Top,Height=72,Padding=new Padding(20,0,0,0),TextAlign=ContentAlignment.MiddleLeft,BackColor=Navy,ForeColor=Color.White,Font=new Font("Segoe UI",18F,FontStyle.Bold)};
        var instructions=new Label{Text=newPolycarbonateCard?"NEW POLYCARBONATE ID - NETUM NT-1228BL: Under PDF417 > Regular/Inverse Decode in the scanner guide, scan Decode Both (called Read Normal Phase / Phase Reversal in the older full manual). Then tilt the reflective card slightly and pull the trigger. If the scanner does not beep, it has not decoded the barcode.":"Connect the NETUM scanner in USB HID / keyboard mode. Make sure PDF417 is enabled, then pull the scanner trigger. You do not need to click a text box.",Dock=DockStyle.Top,Height=88,Padding=new Padding(24,16,24,8),TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.White,ForeColor=Navy,Font=new Font("Segoe UI",11F)};
        var privacy=new Label{Text="The raw barcode is hidden and discarded after parsing. Review the filled bidder fields before saving.",Dock=DockStyle.Bottom,Height=45,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.DimGray};
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=62,FlowDirection=FlowDirection.LeftToRight,Padding=new Padding(155,7,0,7),BackColor=Color.White};actions.Controls.Add(Button("Reset / Scan Again",Reset,true));actions.Controls.Add(Button("Cancel",Close));
        Controls.Add(_status);Controls.Add(privacy);Controls.Add(actions);Controls.Add(instructions);Controls.Add(header);Controls.Add(_scannerInput);_scannerInput.BringToFront();
        _status.Text=_viewModel.Status;_scannerInput.TextChanged+=CaptureScannerInput;_completionTimer.Tick+=(_,_)=>CompleteScan();FormClosed+=(_,_)=>_completionTimer.Dispose();Shown+=(_,_)=>FocusScannerInput();Activated+=(_,_)=>{_status.Text=_viewModel.CharacterCount==0?_viewModel.Status:$"Receiving license barcode... {_viewModel.CharacterCount} characters";BeginInvoke(FocusScannerInput);};
    }

    private void CaptureScannerInput(object? sender,EventArgs eventArgs)
    {
        if(_clearingScannerInput||_scannerInput.TextLength==0)return;
        var characters=_scannerInput.Text;
        _clearingScannerInput=true;
        _scannerInput.Clear();
        _clearingScannerInput=false;
        _viewModel.Receive(characters);
        _waitingForSlowChunk=false;_completionTimer.Interval=HandheldLicenseScanViewModel.InitialCompletionDelayMilliseconds;
        _status.Text=_viewModel.Status;_status.ForeColor=Navy;_completionTimer.Stop();_completionTimer.Start();
    }

    private void FocusScannerInput(){if(!IsDisposed&&_scannerInput.CanFocus)_scannerInput.Focus();}
    private void CompleteScan()
    {
        _completionTimer.Stop();
        if(_viewModel.Complete(_waitingForSlowChunk)){System.Media.SystemSounds.Asterisk.Play();DialogResult=DialogResult.OK;Close();return;}
        _status.Text=_viewModel.Status;
        if(!_waitingForSlowChunk){_waitingForSlowChunk=true;_status.ForeColor=Navy;_completionTimer.Interval=HandheldLicenseScanViewModel.AdditionalChunkDelayMilliseconds;_completionTimer.Start();return;}
        _status.ForeColor=Color.DarkRed;
    }
    private void Reset(){_completionTimer.Stop();_waitingForSlowChunk=false;_completionTimer.Interval=HandheldLicenseScanViewModel.InitialCompletionDelayMilliseconds;_viewModel.Reset();_status.Text=_viewModel.Status;_status.ForeColor=Navy;FocusScannerInput();}
    private static Button Button(string text,Action action,bool accent=false){var button=new Button{Text=text,Width=180,Height=44,Margin=new Padding(6),FlatStyle=FlatStyle.Flat,BackColor=accent?Gold:Blue,ForeColor=accent?Navy:Color.White,Font=new Font("Segoe UI",10F,FontStyle.Bold),Cursor=Cursors.Hand};button.FlatAppearance.BorderColor=accent?Gold:Navy;button.Click+=(_,_)=>action();return button;}
}
