namespace HammerTrackPro2.Services;

public static class DatabaseLocationService
{
    public static string ResolveDatabasePath(WorkstationConfiguration? configuration=null)
    {
        var localFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HammerTrackPro2");
        Directory.CreateDirectory(localFolder);

        configuration ??= WorkstationConfigurationService.Load();
        if (!string.IsNullOrWhiteSpace(configuration.DatabasePath))
        {
            var configuredPath=Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuration.DatabasePath));
            if(configuredPath.StartsWith("\\\\",StringComparison.Ordinal)||IsNetworkDrive(configuredPath))
                throw new InvalidOperationException("The host database must be stored on a local drive. Use Network Setup for the cashier PC instead of a shared database path.");
            Directory.CreateDirectory(Path.GetDirectoryName(configuredPath)!); return configuredPath;
        }

        var sharedDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "HammerTrackPro2");
        Directory.CreateDirectory(sharedDataFolder);
        return Path.Combine(sharedDataFolder, "hammertrack.db");
    }

    private static bool IsNetworkDrive(string path)
    {
        var root=Path.GetPathRoot(path); if(string.IsNullOrWhiteSpace(root))return false;
        try { return new DriveInfo(root).DriveType==DriveType.Network; }
        catch(Exception exception) when(exception is IOException or UnauthorizedAccessException) { return false; }
    }
}
