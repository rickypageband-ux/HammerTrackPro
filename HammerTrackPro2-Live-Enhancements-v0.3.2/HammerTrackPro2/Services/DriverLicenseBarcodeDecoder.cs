using HammerTrackPro2.Models;
using ZXing;
using ZXing.Common;
using OpenCvSharp;
using WindowsBarcodeReader=ZXing.Windows.Compatibility.BarcodeReader;

namespace HammerTrackPro2.Services;

public interface IDriverLicenseBarcodeDecoder
{
    string Status { get; }
    DriverLicenseScanData? Decode(Bitmap frame);
}

public sealed class DriverLicenseBarcodeDecoder : IDriverLicenseBarcodeDecoder
{
    private readonly WindowsBarcodeReader _reader=new(){AutoRotate=true,Options=new DecodingOptions{PossibleFormats=[BarcodeFormat.PDF_417],TryHarder=true,TryInverted=true}};
    private readonly AamvaDriverLicenseParser _parser=new();
    public string Status { get; private set; }="Looking for the PDF417 barcode…";

    public DriverLicenseScanData? Decode(Bitmap frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var result=_reader.Decode(frame);
        if(result is null)
        {
            foreach(var enhanced in CreateEnhancedFrames(frame))
            {
                using(enhanced){result=_reader.Decode(enhanced);if(result is not null)break;}
            }
        }
        if(result is null){Status="Barcode not read yet — move it closer, keep it level, and hold steady";return null;}
        try{var parsed=_parser.Parse(result.Text);Status="Driver's license barcode recognized";return parsed;}
        catch(InvalidOperationException exception){Status=$"Barcode found, but license data was not recognized: {exception.Message}";return null;}
    }

    private static IEnumerable<Bitmap> CreateEnhancedFrames(Bitmap frame)
    {
        using var stream=new MemoryStream();frame.Save(stream,System.Drawing.Imaging.ImageFormat.Bmp);using var source=Cv2.ImDecode(stream.ToArray(),ImreadModes.Color);
        if(source.Empty())yield break;
        using var enlarged=new Mat();Cv2.Resize(source,enlarged,new OpenCvSharp.Size(),1.6,1.6,InterpolationFlags.Cubic);
        using var gray=new Mat();Cv2.CvtColor(enlarged,gray,ColorConversionCodes.BGR2GRAY);
        using var contrast=new Mat();using(var clahe=Cv2.CreateCLAHE(3.0,new OpenCvSharp.Size(8,8)))clahe.Apply(gray,contrast);
        yield return ToBitmap(contrast);
        using var threshold=new Mat();Cv2.AdaptiveThreshold(contrast,threshold,255,AdaptiveThresholdTypes.GaussianC,ThresholdTypes.Binary,31,7);
        yield return ToBitmap(threshold);
    }

    private static Bitmap ToBitmap(Mat frame){Cv2.ImEncode(".bmp",frame,out var bytes);using var stream=new MemoryStream(bytes);using var image=new Bitmap(stream);return new Bitmap(image);}
}
