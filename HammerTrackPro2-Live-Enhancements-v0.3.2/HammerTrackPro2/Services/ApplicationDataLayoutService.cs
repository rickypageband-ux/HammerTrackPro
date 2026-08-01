namespace HammerTrackPro2.Services;

public static class ApplicationDataLayoutService
{
    public const string ProductFolderName = "HammerTrackPro";
    public const string LegacyProductFolderName = "HammerTrackPro2";

    public static string RootFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),ProductFolderName);
    public static string BackupsFolder => Path.Combine(RootFolder,"Backups");
    public static string LogsFolder => Path.Combine(RootFolder,"Logs");
    public static string SettingsFolder => Path.Combine(RootFolder,"Settings");
    public static string DefaultDatabaseFile => Path.Combine(RootFolder,"hammertrack.db");
    public static string WorkstationConfigurationFile => Path.Combine(SettingsFolder,"workstation.json");

    public static void EnsureAndMigrateLegacyData()
    {
        var legacyRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),LegacyProductFolderName);
        var legacyConfiguration=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),LegacyProductFolderName,"workstation.json");
        EnsureAndMigrateLegacyData(RootFolder,legacyRoot,legacyConfiguration);
    }

    internal static void EnsureAndMigrateLegacyData(string rootFolder,string legacyRoot,string legacyConfiguration)
    {
        var backupsFolder=Path.Combine(rootFolder,"Backups"); var logsFolder=Path.Combine(rootFolder,"Logs"); var settingsFolder=Path.Combine(rootFolder,"Settings"); var databaseFile=Path.Combine(rootFolder,"hammertrack.db");
        Directory.CreateDirectory(rootFolder); Directory.CreateDirectory(backupsFolder); Directory.CreateDirectory(logsFolder); Directory.CreateDirectory(settingsFolder);
        if(!File.Exists(databaseFile))
        {
            CopyIfMissing(Path.Combine(legacyRoot,"hammertrack.db"),databaseFile);
            CopyIfMissing(Path.Combine(legacyRoot,"hammertrack.db-wal"),databaseFile+"-wal");
            CopyIfMissing(Path.Combine(legacyRoot,"hammertrack.db-shm"),databaseFile+"-shm");
        }
        CopyDirectoryIfMissing(Path.Combine(legacyRoot,"Backups"),backupsFolder);
        CopyIfMissing(legacyConfiguration,Path.Combine(settingsFolder,"workstation.json"));
    }

    private static void CopyIfMissing(string source,string destination)
    {
        if(!File.Exists(source)||File.Exists(destination))return;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(source,destination,false);
    }

    private static void CopyDirectoryIfMissing(string source,string destination)
    {
        if(!Directory.Exists(source))return;
        foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories))
        {
            var relative=Path.GetRelativePath(source,file); CopyIfMissing(file,Path.Combine(destination,relative));
        }
    }
}
