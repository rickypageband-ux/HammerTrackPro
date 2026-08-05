using HammerTrackPro2.Models;
using ZXing;
using ZXing.Common;
using WindowsBarcodeReader=ZXing.Windows.Compatibility.BarcodeReader;

namespace HammerTrackPro2.Services;

public interface IDriverLicenseBarcodeDecoder
{
    DriverLicenseScanData? Decode(Bitmap frame);
}

public sealed class DriverLicenseBarcodeDecoder : IDriverLicenseBarcodeDecoder
{
    private readonly WindowsBarcodeReader _reader=new(){AutoRotate=true,Options=new DecodingOptions{PossibleFormats=[BarcodeFormat.PDF_417],TryHarder=true,TryInverted=true}};
    private readonly AamvaDriverLicenseParser _parser=new();

    public DriverLicenseScanData? Decode(Bitmap frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var result=_reader.Decode(frame);if(result is null)return null;
        try{return _parser.Parse(result.Text);}catch(InvalidOperationException){return null;}
    }
}
