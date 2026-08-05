using OpenCvSharp;

namespace HammerTrackPro2.Services;

public sealed class CameraCaptureSession : IDisposable
{
    private VideoCapture? _capture;
    public int CameraIndex { get; private set; }=-1;

    public void OpenFirstAvailable(int startingIndex=0)
    {
        DisposeCapture();
        for(var offset=0;offset<5;offset++)
        {
            var index=(startingIndex+offset)%5;var capture=new VideoCapture(index,VideoCaptureAPIs.DSHOW);
            if(capture.IsOpened()){capture.Set(VideoCaptureProperties.FrameWidth,1920);capture.Set(VideoCaptureProperties.FrameHeight,1080);capture.Set(VideoCaptureProperties.AutoFocus,1);_capture=capture;CameraIndex=index;return;}
            capture.Dispose();
        }
        throw new InvalidOperationException("No available camera was found. Connect a camera, close other apps using it, and try again.");
    }

    public Bitmap? ReadFrame()
    {
        if(_capture is null||!_capture.IsOpened())return null;
        using var frame=new Mat();if(!_capture.Read(frame)||frame.Empty())return null;
        Cv2.ImEncode(".bmp",frame,out var bytes);using var stream=new MemoryStream(bytes);using var image=new Bitmap(stream);return new Bitmap(image);
    }

    public void Dispose(){DisposeCapture();GC.SuppressFinalize(this);}
    private void DisposeCapture(){_capture?.Release();_capture?.Dispose();_capture=null;CameraIndex=-1;}
}
