using HammerTrackPro2.Services;

namespace HammerTrackPro2.Tests;

public sealed class UserManualServiceTests
{
    [Fact]
    public void ResolveManualPath_PrefersOfflineInstalledManual()
    {
        var folder=Path.Combine(Path.GetTempPath(),"HammerTrackPro2.Tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{var expected=Path.Combine(folder,UserManualService.FileName);File.WriteAllText(expected,"manual");Assert.Equal(expected,new UserManualService(folder).ResolveManualPath());}
        finally{Directory.Delete(folder,true);}
    }

    [Fact]
    public void ResolveManualPath_GivesActionableMessageWhenMissing()
    {
        var folder=Path.Combine(Path.GetTempPath(),"HammerTrackPro2.Tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{var exception=Assert.Throws<FileNotFoundException>(()=>new UserManualService(folder).ResolveManualPath());Assert.Contains("not installed",exception.Message);Assert.Contains("hammertrackpro.com",exception.Message);}
        finally{Directory.Delete(folder,true);}
    }
}
