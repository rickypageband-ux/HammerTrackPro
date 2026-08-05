using System.Drawing;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
using ZXing;
using ZXing.Common;
using WindowsBarcodeWriter=ZXing.Windows.Compatibility.BarcodeWriter;
using OpenCvSharp;

namespace HammerTrackPro2.Tests;

public sealed class DriverLicenseScannerTests
{
    private const string Sample="@\nANSI 636026080102DL00410288ZA03290015DLDAQPA12345678\nDCSDOE\nDACJANE\nDADMARIE\nDAG123 MAIN STREET\nDAIYORK\nDAJPA\nDAK174010000\n";

    [Fact]
    public void Parser_MapsAamvaIdentityAndAddressWithoutRetainingRawBarcode()
    {
        var result=new AamvaDriverLicenseParser().Parse(Sample);
        Assert.Equal("JANE",result.FirstName);Assert.Equal("DOE",result.LastName);Assert.Equal("123 MAIN STREET",result.Address);Assert.Equal("YORK",result.City);Assert.Equal("PA",result.State);Assert.Equal("17401-0000",result.Zip);Assert.Equal("PA12345678",result.DriversLicense);
        Assert.DoesNotContain("ANSI",string.Join('|',result.FirstName,result.LastName,result.Address,result.City,result.State,result.Zip,result.DriversLicense));
    }

    [Fact]
    public void Parser_RejectsNonLicensePdf417Payload()
    {
        var exception=Assert.Throws<InvalidOperationException>(()=>new AamvaDriverLicenseParser().Parse("ordinary PDF417 data"));
        Assert.Contains("not an AAMVA",exception.Message);
    }

    [Fact]
    public void Decoder_ReadsGeneratedPdf417AndViewModelReturnsParsedResult()
    {
        var writer=new WindowsBarcodeWriter{Format=BarcodeFormat.PDF_417,Options=new EncodingOptions{Width=900,Height=300,Margin=20}};
        using var barcode=writer.Write(Sample);
        var viewModel=new DriverLicenseScanViewModel(new DriverLicenseBarcodeDecoder());
        Assert.True(viewModel.ProcessFrame(barcode));Assert.Equal("PA12345678",viewModel.Result!.DriversLicense);
    }

    [Fact]
    public void ViewModel_LeavesResultEmptyUntilDecoderFindsLicense()
    {
        using var bitmap=new Bitmap(10,10);var viewModel=new DriverLicenseScanViewModel(new EmptyDecoder());
        Assert.False(viewModel.ProcessFrame(bitmap));Assert.Null(viewModel.Result);
    }

    [Fact]
    public void PackagedCameraRuntime_LoadsWithoutOpeningHardware()
    {
        Assert.False(string.IsNullOrWhiteSpace(Cv2.GetVersionString()));
        using var session=new CameraCaptureSession();Assert.Null(session.ReadFrame());
    }

    private sealed class EmptyDecoder : IDriverLicenseBarcodeDecoder{public DriverLicenseScanData? Decode(Bitmap frame)=>null;}
}
