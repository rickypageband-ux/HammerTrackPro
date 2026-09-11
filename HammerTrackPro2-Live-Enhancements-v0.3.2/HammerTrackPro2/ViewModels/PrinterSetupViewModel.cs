using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class PrinterSetupViewModel
{
    private readonly IPrinterCatalog _catalog;
    private readonly IPrinterPreferenceStore _store;

    public PrinterSetupViewModel(IPrinterCatalog catalog, IPrinterPreferenceStore store)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Printers = _catalog.GetInstalledPrinters();
        var saved = _store.PrinterName;
        SelectedPrinter = Printers.FirstOrDefault(name => string.Equals(name, saved, StringComparison.OrdinalIgnoreCase))
            ?? Printers.FirstOrDefault(name => string.Equals(name, _catalog.GetDefaultPrinter(), StringComparison.OrdinalIgnoreCase))
            ?? Printers.FirstOrDefault()
            ?? string.Empty;
    }

    public IReadOnlyList<string> Printers { get; }
    public string SelectedPrinter { get; set; }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(SelectedPrinter))
            throw new InvalidOperationException("No printer is installed. Install the printer in Windows, then reopen Printer Setup.");
        if (!Printers.Contains(SelectedPrinter, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose an installed printer from the list.");
        _store.Save(SelectedPrinter);
    }
}
