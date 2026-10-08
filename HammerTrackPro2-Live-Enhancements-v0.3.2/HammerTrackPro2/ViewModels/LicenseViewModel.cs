using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class LicenseViewModel
{
    private readonly LicenseAccess? _access;
    public LicenseViewModel(LicenseAccess? access = null) => _access = access ?? LicenseSession.Current;
    public bool IsActive => _access?.IsActive ?? true;
    public string Status => _access is null ? "Import a paid or seven-day demo license for this PC."
        : _access.IsDemo ? $"SEVEN-DAY DEMO — expires {_access.License.ExpiresUtc!.Value.ToLocalTime():g}. Scanner and Wholesale disabled."
        : $"Licensed to {_access.License.CustomerName}";
    public bool CanUseFeature(string feature, out string reason)
    {
        try { _access?.EnsureFullFeature(feature); reason = string.Empty; return true; }
        catch (InvalidOperationException exception) { reason = exception.Message; return false; }
    }
    public string MachineId => LicenseService.GetMachineId();
    public LicensePayload Import(string path) => LicenseService.InstallLicense(path);
}
