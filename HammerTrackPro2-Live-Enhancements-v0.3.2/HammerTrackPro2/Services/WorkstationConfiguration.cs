using System.Security.Cryptography;
using System.Text.Json;

namespace HammerTrackPro2.Services;

public enum WorkstationRole { Host, Cashier }

public sealed class WorkstationConfiguration
{
    public WorkstationRole Role { get; set; } = WorkstationRole.Host;
    public string DatabasePath { get; set; } = string.Empty;
    public int ServicePort { get; set; } = 7443;
    public string PairingKey { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string HostUrl { get; set; } = string.Empty;
}

public static class WorkstationConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions=new() { WriteIndented=true };
    public static string ConfigurationFile { get { ApplicationDataLayoutService.EnsureAndMigrateLegacyData(); return ApplicationDataLayoutService.WorkstationConfigurationFile; } }
    public static bool Exists => File.Exists(ConfigurationFile);
    public static WorkstationConfiguration Load()
    {
        if(!File.Exists(ConfigurationFile)) return CreateHostDefaults();
        try
        {
            var configuration=JsonSerializer.Deserialize<WorkstationConfiguration>(File.ReadAllText(ConfigurationFile)) ?? CreateHostDefaults();
            if(string.IsNullOrWhiteSpace(configuration.PairingKey)) configuration.PairingKey=GenerateKey();
            if(configuration.ServicePort is <1024 or >65535) configuration.ServicePort=7443;
            return configuration;
        }
        catch(JsonException) { return CreateHostDefaults(); }
    }
    public static void Save(WorkstationConfiguration configuration)
    {
        var folder=Path.GetDirectoryName(ConfigurationFile)!; Directory.CreateDirectory(folder);
        File.WriteAllText(ConfigurationFile,JsonSerializer.Serialize(configuration,JsonOptions));
    }
    public static WorkstationConfiguration CreateHostDefaults() => new() { Role=WorkstationRole.Host,ServicePort=7443,PairingKey=GenerateKey() };
    private static string GenerateKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
