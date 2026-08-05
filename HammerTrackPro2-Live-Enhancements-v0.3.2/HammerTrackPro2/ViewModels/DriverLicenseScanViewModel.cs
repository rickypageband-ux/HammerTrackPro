using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class DriverLicenseScanViewModel
{
    private readonly IDriverLicenseBarcodeDecoder _decoder;
    public DriverLicenseScanViewModel(IDriverLicenseBarcodeDecoder decoder)=>_decoder=decoder??throw new ArgumentNullException(nameof(decoder));
    public DriverLicenseScanData? Result { get; private set; }
    public bool ProcessFrame(Bitmap frame){Result=_decoder.Decode(frame);return Result is not null;}
}
