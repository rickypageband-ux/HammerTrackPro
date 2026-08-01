using System.Text.Json;

namespace HammerTrackPro2.Services;

public static class DatabaseLocationService
{
    private sealed class WorkstationConfiguration
    {
        public string DatabasePath { get; set; } = string.Empty;
    }

    public static string ResolveDatabasePath()
    {
        var localFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HammerTrackPro2");
        Directory.CreateDirectory(localFolder);

        var configurationFile = Path.Combine(localFolder, "workstation.json");
        if (File.Exists(configurationFile))
        {
            try
            {
                var configuration = JsonSerializer.Deserialize<WorkstationConfiguration>(File.ReadAllText(configurationFile));
                if (!string.IsNullOrWhiteSpace(configuration?.DatabasePath))
                {
                    var configuredPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuration.DatabasePath));
                    Directory.CreateDirectory(Path.GetDirectoryName(configuredPath)!);
                    return configuredPath;
                }
            }
            catch (JsonException)
            {
                // Fall back to the standard local database path.
            }
        }

        var sharedDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "HammerTrackPro2");
        Directory.CreateDirectory(sharedDataFolder);
        return Path.Combine(sharedDataFolder, "hammertrack.db");
    }
}
