using System.Diagnostics;

namespace HammerTrackPro2.Services;

public interface IUserManualService
{
    string ResolveManualPath();
    void OpenManual();
}

public sealed class UserManualService : IUserManualService
{
    public const string FileName="HammerTrackPro-User-Manual.pdf";
    private readonly string _applicationFolder;

    public UserManualService(string? applicationFolder=null)=>_applicationFolder=applicationFolder??AppContext.BaseDirectory;

    public string ResolveManualPath()
    {
        var installed=Path.Combine(_applicationFolder,FileName);
        if(File.Exists(installed))return installed;
        var development=Path.GetFullPath(Path.Combine(_applicationFolder,"..","..","..","..","output","pdf",FileName));
        if(File.Exists(development))return development;
        throw new FileNotFoundException("The offline HammerTrack Pro User Manual is not installed. Reinstall HammerTrack Pro or visit https://hammertrackpro.com for support.",installed);
    }

    public void OpenManual()=>Process.Start(new ProcessStartInfo(ResolveManualPath()){UseShellExecute=true});
}
