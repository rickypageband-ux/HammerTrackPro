using System.Drawing.Printing;

namespace HammerTrackPro2.Services;

public interface IPrinterCatalog
{
    IReadOnlyList<string> GetInstalledPrinters();
    string GetDefaultPrinter();
}

public sealed class WindowsPrinterCatalog : IPrinterCatalog
{
    public IReadOnlyList<string> GetInstalledPrinters() =>
        PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(name => name).ToList();

    public string GetDefaultPrinter() => new PrinterSettings().PrinterName ?? string.Empty;
}

public interface IPrinterPreferenceStore
{
    string PrinterName { get; }
    void Save(string printerName);
}

public sealed class WorkstationPrinterPreferenceStore : IPrinterPreferenceStore
{
    public string PrinterName => WorkstationConfigurationService.Load().PrinterName;

    public void Save(string printerName)
    {
        var configuration = WorkstationConfigurationService.Load();
        configuration.PrinterName = printerName;
        WorkstationConfigurationService.Save(configuration);
    }
}

public static class PrinterPreferenceService
{
    public static void Apply(PrintDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var configuration = WorkstationConfigurationService.Load();
        if (string.IsNullOrWhiteSpace(configuration.PrinterName)) return;

        var installed = PrinterSettings.InstalledPrinters.Cast<string>();
        if (!installed.Contains(configuration.PrinterName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"The saved printer '{configuration.PrinterName}' is not available on this computer. Open Printer Setup and choose an installed printer.");

        document.PrinterSettings.PrinterName = configuration.PrinterName;
        if (!document.PrinterSettings.IsValid)
            throw new InvalidOperationException(
                $"Windows cannot use the saved printer '{configuration.PrinterName}'. Check that it is installed and online, then choose it again in Printer Setup.");
    }
}
