using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class DriverLicenseScannerForm : Form
{
    private static readonly Color Navy=Color.FromArgb(24,50,74),Gold=Color.FromArgb(224,168,54);
    private readonly DriverLicenseScanViewModel _viewModel=new(new DriverLicenseBarcodeDecoder());
    private readonly CameraCaptureSession _camera=new();
    private readonly PictureBox _preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.Black};
    private readonly Label _status=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.White,Font=new Font("Segoe UI",10F,FontStyle.Bold)};
    private readonly System.Windows.Forms.Timer _timer=new(){Interval=160};
    private int _frameCount;
    private bool _processing;

    public DriverLicenseScanData? Result=>_viewModel.Result;

    public DriverLicenseScannerForm()
    {
        Text="HammerTrack Pro — Scan Driver's License";Width=980;Height=720;MinimumSize=new Size(760,560);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(242,245,248);
        var header=new Panel{Dock=DockStyle.Top,Height=74,BackColor=Navy,Padding=new Padding(18,8,14,8)};
        header.Controls.Add(new Label{Text="SCAN DRIVER'S LICENSE",Dock=DockStyle.Left,Width=360,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.White,Font=new Font("Segoe UI",18F,FontStyle.Bold)});
        var switchCamera=Button("Next Camera",SwitchCamera);switchCamera.Dock=DockStyle.Right;var cancel=Button("Cancel",Close);cancel.Dock=DockStyle.Right;header.Controls.Add(cancel);header.Controls.Add(switchCamera);
        var instruction=new Label{Text="Hold the PDF417 barcode on the back of the license steady inside the camera view. The window closes automatically after a successful scan.",Dock=DockStyle.Top,Height=58,Padding=new Padding(18,12,18,8),Font=new Font("Segoe UI",10.5F),ForeColor=Navy,BackColor=Color.White};
        var statusPanel=new Panel{Dock=DockStyle.Bottom,Height=50,BackColor=Navy,Padding=new Padding(18,4,18,4)};_status.Text="Starting camera…";statusPanel.Controls.Add(_status);
        _preview.Paint+=(_,e)=>{var insetX=Math.Max(30,_preview.ClientSize.Width/10);var insetY=Math.Max(30,_preview.ClientSize.Height/5);using var pen=new Pen(Gold,3F){DashStyle=System.Drawing.Drawing2D.DashStyle.Dash};e.Graphics.DrawRectangle(pen,insetX,insetY,Math.Max(1,_preview.ClientSize.Width-insetX*2),Math.Max(1,_preview.ClientSize.Height-insetY*2));};
        Controls.Add(_preview);Controls.Add(statusPanel);Controls.Add(instruction);Controls.Add(header);
        Shown+=(_,_)=>StartCamera();_timer.Tick+=(_,_)=>CaptureFrame();FormClosed+=(_,_)=>StopCamera();
    }

    private void StartCamera(){try{_camera.OpenFirstAvailable();_status.Text=$"Camera {_camera.CameraIndex+1} ready — hold the barcode steady";_timer.Start();}catch(Exception exception){_status.Text=exception.Message;MessageBox.Show(this,exception.Message,"Camera",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    private void SwitchCamera(){try{_timer.Stop();_camera.OpenFirstAvailable(_camera.CameraIndex+1);_status.Text=$"Camera {_camera.CameraIndex+1} ready — hold the barcode steady";_timer.Start();}catch(Exception exception){_status.Text=exception.Message;}}
    private void CaptureFrame()
    {
        if(_processing)return;_processing=true;
        try
        {
            using var frame=_camera.ReadFrame();if(frame is null)return;
            var prior=_preview.Image;_preview.Image=new Bitmap(frame);prior?.Dispose();
            _frameCount++;if(_frameCount%2!=0)return;
            if(_viewModel.ProcessFrame(frame)){_status.Text="Driver's license scanned — filling bidder information";DialogResult=DialogResult.OK;Close();}
            else _status.Text=_viewModel.Status;
        }
        catch(Exception exception){_status.Text=$"Camera read error: {exception.Message}";}
        finally{_processing=false;}
    }
    private void StopCamera(){_timer.Stop();_timer.Dispose();_camera.Dispose();var image=_preview.Image;_preview.Image=null;image?.Dispose();}
    private static Button Button(string text,Action action){var button=new Button{Text=text,Width=135,Height=42,Margin=new Padding(6),FlatStyle=FlatStyle.Flat,BackColor=Gold,ForeColor=Navy,Font=new Font("Segoe UI",10F,FontStyle.Bold),Cursor=Cursors.Hand};button.FlatAppearance.BorderColor=Gold;button.Click+=(_,_)=>action();return button;}
}
