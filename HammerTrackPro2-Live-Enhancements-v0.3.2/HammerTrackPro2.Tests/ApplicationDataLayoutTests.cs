using HammerTrackPro2.Services;

namespace HammerTrackPro2.Tests;

public sealed class ApplicationDataLayoutTests
{
    [Fact]
    public void Migration_CopiesLegacyDataWithoutOverwritingNewData()
    {
        var root=Path.Combine(Path.GetTempPath(),"HammerTrackPro2.Tests",Guid.NewGuid().ToString("N"));
        try
        {
            var current=Path.Combine(root,"current"); var legacy=Path.Combine(root,"legacy"); var legacySettings=Path.Combine(root,"legacy-workstation.json");
            Directory.CreateDirectory(Path.Combine(legacy,"Backups")); File.WriteAllText(Path.Combine(legacy,"hammertrack.db"),"legacy-db"); File.WriteAllText(Path.Combine(legacy,"Backups","auction.htpbackup"),"backup"); File.WriteAllText(legacySettings,"legacy-settings");
            ApplicationDataLayoutService.EnsureAndMigrateLegacyData(current,legacy,legacySettings);
            Assert.Equal("legacy-db",File.ReadAllText(Path.Combine(current,"hammertrack.db")));
            Assert.True(File.Exists(Path.Combine(current,"Backups","auction.htpbackup")),"Legacy backup was not copied.");
            Assert.True(Directory.Exists(Path.Combine(current,"Logs")),"Logs directory was not created.");
            Assert.Equal("legacy-settings",File.ReadAllText(Path.Combine(current,"Settings","workstation.json")));
            File.WriteAllText(Path.Combine(current,"hammertrack.db"),"current-db"); ApplicationDataLayoutService.EnsureAndMigrateLegacyData(current,legacy,legacySettings);
            Assert.Equal("current-db",File.ReadAllText(Path.Combine(current,"hammertrack.db")));
        }
        finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }
}
