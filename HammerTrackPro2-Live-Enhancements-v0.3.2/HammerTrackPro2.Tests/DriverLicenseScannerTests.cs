using System.Drawing;
using System.Drawing.Imaging;
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
    [Fact]
    public void ScannerFormProvidesPrivateKeyboardWedgeReceiverForZebraAndNetumStreams()
    {
        using var form=new HammerTrackPro2.Forms.HandheldLicenseScannerForm();
        Assert.True(form.ScannerInputControl.Multiline);
        Assert.True(form.ScannerInputControl.AcceptsReturn);
        Assert.True(form.ScannerInputControl.AcceptsTab);
        Assert.True(form.ScannerInputControl.ShortcutsEnabled);
        Assert.False(form.ScannerInputControl.TabStop);
    }

    [Fact]
    public void NewIdScanner_ExplainsWhenHardwareSendsNoBarcodeCharacters()
    {
        var viewModel=new HandheldLicenseScanViewModel();
        viewModel.ReportNoInputReceived();
        Assert.Contains("No barcode characters reached HammerTrack",viewModel.Status);
        Assert.Contains("Decode Both",viewModel.Status);
        Assert.Equal(0,viewModel.CharacterCount);
    }

    private const string Sample="@\nANSI 636026080102DL00410288ZA03290015DLDAQPA12345678\nDCSDOE\nDACJANE\nDADMARIE\nDAG123 MAIN STREET\nDAIYORK\nDAJPA\nDAK174010000\n";

    [Fact]
    public void HandheldScanner_AllowsSlowComputersToFinishTheBarcodeBeforeParsing()
    {
        Assert.Equal(550,HandheldLicenseScanViewModel.InitialCompletionDelayMilliseconds);
        Assert.True(HandheldLicenseScanViewModel.AdditionalChunkDelayMilliseconds>=1500);
        var split=Sample.IndexOf("DAQ",StringComparison.Ordinal);var viewModel=new HandheldLicenseScanViewModel();viewModel.Receive(Sample[..split]);
        Assert.False(viewModel.Complete(false));Assert.True(viewModel.CharacterCount>0);Assert.Contains("Waiting",viewModel.Status);
        viewModel.Receive(Sample[split..]);Assert.True(viewModel.Complete());Assert.Equal("JANE",viewModel.Result!.FirstName);
    }

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
    public void Decoder_ReadsLowContrastLicenseBarcodeInsideCameraSizedFrame()
    {
        var writer=new WindowsBarcodeWriter{Format=BarcodeFormat.PDF_417,Options=new EncodingOptions{Width=900,Height=300,Margin=20}};
        using var barcode=writer.Write(Sample);using var frame=new Bitmap(1920,1080);using var graphics=Graphics.FromImage(frame);graphics.Clear(Color.LightGray);
        using var attributes=new ImageAttributes();attributes.SetColorMatrix(new ColorMatrix(new[]{new[]{0.32F,0F,0F,0F,0F},new[]{0F,0.32F,0F,0F,0F},new[]{0F,0F,0.32F,0F,0F},new[]{0F,0F,0F,1F,0F},new[]{0.38F,0.38F,0.38F,0F,1F}}));
        graphics.DrawImage(barcode,new Rectangle(480,380,960,320),0,0,barcode.Width,barcode.Height,GraphicsUnit.Pixel,attributes);
        var result=new DriverLicenseBarcodeDecoder().Decode(frame);Assert.NotNull(result);Assert.Equal("PA12345678",result!.DriversLicense);
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

    [Fact]
    public void HandheldScanner_CollectsKeyboardStreamAndParsesAfterPause()
    {
        var viewModel=new HandheldLicenseScanViewModel();foreach(var character in Sample)viewModel.Receive(character);viewModel.Receive('\r');
        Assert.True(viewModel.CharacterCount>100);Assert.True(viewModel.Complete());Assert.Equal("JANE",viewModel.Result!.FirstName);Assert.Equal("PA12345678",viewModel.Result.DriversLicense);
        Assert.Equal("Driver's license scanned",viewModel.Status);
    }

    [Fact]
    public void HandheldScanner_RejectsOtherBarcodeAndAllowsImmediateRescan()
    {
        var viewModel=new HandheldLicenseScanViewModel();foreach(var character in "NOT A LICENSE")viewModel.Receive(character);
        Assert.False(viewModel.Complete());Assert.Equal(0,viewModel.CharacterCount);Assert.Contains("PDF417",viewModel.Status);
        foreach(var character in Sample)viewModel.Receive(character);Assert.True(viewModel.Complete());
    }

    [Fact]
    public void HandheldScanner_ParsesNetumStreamWhenLineSeparatorsAreRemoved()
    {
        var collapsed=Sample.Replace("\n",string.Empty,StringComparison.Ordinal);
        var viewModel=new HandheldLicenseScanViewModel();foreach(var character in collapsed)viewModel.Receive(character);
        Assert.True(viewModel.Complete());Assert.Equal("JANE",viewModel.Result!.FirstName);Assert.Equal("DOE",viewModel.Result.LastName);Assert.Equal("PA12345678",viewModel.Result.DriversLicense);
    }

    [Fact]
    public void HandheldScanner_TreatsScannerTabsAsLicenseFieldSeparators()
    {
        var tabSeparated=Sample.Replace('\n','\t');
        var viewModel=new HandheldLicenseScanViewModel();foreach(var character in tabSeparated)viewModel.Receive(character);
        Assert.True(viewModel.Complete());Assert.Equal("YORK",viewModel.Result!.City);Assert.Equal("17401-0000",viewModel.Result.Zip);
    }

    [Fact]
    public void HandheldScanner_AcceptsEntirePastedScannerPayload()
    {
        var viewModel=new HandheldLicenseScanViewModel();viewModel.Receive(Sample.Replace('\n','\t'));
        Assert.True(viewModel.CharacterCount>100);Assert.True(viewModel.Complete());Assert.Equal("JANE",viewModel.Result!.FirstName);Assert.Equal("PA12345678",viewModel.Result.DriversLicense);
    }

    [Fact]
    public void Parser_StopsLicenseNumberAtOptionalAamvaFieldsInCollapsedNetumStream()
    {
        const string netum="ANSI 636026080102DLDAQ8406290DCF2000200102245974463DCGUSADDEUDDFUDDGUDCLW  DCM****DCRA - Corrective LensDDAFDDB01012018DAW280DCSDAVIDDACRICHARDDADWILLIAM";
        var result=new AamvaDriverLicenseParser().Parse(netum);
        Assert.Equal("8406290",result.DriversLicense);Assert.Equal("DAVID",result.LastName);Assert.Equal("RICHARD",result.FirstName);
    }

    [Fact]
    public void Parser_DoesNotStartAFieldInsideANameEndingInDa()
    {
        const string netum="ANSI 636026080102DLDAQ123456DCFCONTROLDCSSMITHDACWANDADADMARIE";
        var result=new AamvaDriverLicenseParser().Parse(netum);
        Assert.Equal("SMITH",result.LastName);Assert.Equal("WANDA",result.FirstName);
    }

    private sealed class EmptyDecoder : IDriverLicenseBarcodeDecoder{public string Status=>"No barcode";public DriverLicenseScanData? Decode(Bitmap frame)=>null;}
}
