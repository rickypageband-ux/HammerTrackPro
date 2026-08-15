using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class WholesaleActivationViewModel
{
    private readonly IModuleEntitlementService _entitlements;
    public WholesaleActivationViewModel(IModuleEntitlementService entitlements)=>_entitlements=entitlements??throw new ArgumentNullException(nameof(entitlements));
    public string MachineId=>LicenseService.GetMachineId();
    public string Activate(string key)
    {
        if(string.IsNullOrWhiteSpace(key))throw new InvalidOperationException("Paste the Wholesale Auction activation key.");
        var activation=_entitlements.Activate(key,ProductModules.Wholesale);
        return $"Wholesale Auction is activated for {activation.CustomerName}.";
    }
}
