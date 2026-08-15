using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class HelpViewModel
{
    private readonly IUserManualService _manualService;
    public HelpViewModel(IUserManualService manualService)=>_manualService=manualService??throw new ArgumentNullException(nameof(manualService));
    public void OpenUserManual()=>_manualService.OpenManual();
}
