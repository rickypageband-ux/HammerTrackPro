using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Tests;

public sealed class PrinterSetupViewModelTests
{
    [Fact]
    public void UsesSavedPrinterAndPersistsChangedSelection()
    {
        var store = new FakeStore("Receipt Printer");
        var viewModel = new PrinterSetupViewModel(new FakeCatalog(["Office Printer", "Receipt Printer"], "Office Printer"), store);

        Assert.Equal("Receipt Printer", viewModel.SelectedPrinter);
        viewModel.SelectedPrinter = "Office Printer";
        viewModel.Save();

        Assert.Equal("Office Printer", store.PrinterName);
    }

    [Fact]
    public void FallsBackToWindowsDefaultWhenNoSavedPrinterExists()
    {
        var viewModel = new PrinterSetupViewModel(new FakeCatalog(["Office Printer", "Receipt Printer"], "Receipt Printer"), new FakeStore(string.Empty));
        Assert.Equal("Receipt Printer", viewModel.SelectedPrinter);
    }

    [Fact]
    public void RejectsPrinterThatIsNotInstalled()
    {
        var viewModel = new PrinterSetupViewModel(new FakeCatalog(["Office Printer"], "Office Printer"), new FakeStore(string.Empty)) { SelectedPrinter = "Missing Printer" };
        var error = Assert.Throws<InvalidOperationException>(viewModel.Save);
        Assert.Contains("installed printer", error.Message);
    }

    private sealed class FakeCatalog(IReadOnlyList<string> printers, string defaultPrinter) : IPrinterCatalog
    {
        public IReadOnlyList<string> GetInstalledPrinters() => printers;
        public string GetDefaultPrinter() => defaultPrinter;
    }

    private sealed class FakeStore(string printerName) : IPrinterPreferenceStore
    {
        public string PrinterName { get; private set; } = printerName;
        public void Save(string printerName) => PrinterName = printerName;
    }
}
