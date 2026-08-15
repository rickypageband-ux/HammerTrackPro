using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HammerTrackPro2.Services;

public static class ProductModules
{
    public const string Wholesale="Wholesale";
}

public sealed record ModuleActivationPayload(string ActivationId,string Module,string CustomerName,string[] MachineIds,DateTime IssuedUtc,DateTime? ExpiresUtc);
public sealed record ModuleActivationDocument(ModuleActivationPayload Payload,string Signature);

public interface IModuleEntitlementService
{
    bool IsActivated(string module,out string reason);
    ModuleActivationPayload Activate(string activationKey,string module);
}

public sealed class ModuleEntitlementService : IModuleEntitlementService
{
    private static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    private static string ActivationFile(string module)=>Path.Combine(ApplicationDataLayoutService.SettingsFolder,$"module-{module.ToLowerInvariant()}.htpactivation");

    public bool IsActivated(string module,out string reason)
    {
        ApplicationDataLayoutService.EnsureAndMigrateLegacyData();
        var path=ActivationFile(module);
        if(!File.Exists(path)){reason=$"The {module} Auction module has not been activated on this computer.";return false;}
        try{return ValidateKey(File.ReadAllText(path),module,LicenseService.GetMachineId(),LicenseService.PublicKeyPem,DateTime.UtcNow,out _,out reason);}
        catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or JsonException or CryptographicException){reason="The saved module activation could not be read or is invalid.";return false;}
    }

    public ModuleActivationPayload Activate(string activationKey,string module)
    {
        if(!ValidateKey(activationKey,module,LicenseService.GetMachineId(),LicenseService.PublicKeyPem,DateTime.UtcNow,out var payload,out var reason))throw new InvalidOperationException(reason);
        Directory.CreateDirectory(ApplicationDataLayoutService.SettingsFolder);
        File.WriteAllText(ActivationFile(module),activationKey.Trim());
        return payload!;
    }

    internal static bool ValidateKey(string activationKey,string module,string machineId,string publicKeyPem,DateTime nowUtc,out ModuleActivationPayload? payload,out string error)
    {
        payload=null;
        try
        {
            var json=Encoding.UTF8.GetString(Convert.FromBase64String(NormalizeBase64(activationKey)));
            var document=JsonSerializer.Deserialize<ModuleActivationDocument>(json,JsonOptions);
            if(document?.Payload is null||string.IsNullOrWhiteSpace(document.Signature)){error="The activation key is incomplete.";return false;}
            var payloadJson=JsonSerializer.Serialize(document.Payload,JsonOptions);
            using var rsa=RSA.Create();rsa.ImportFromPem(publicKeyPem);
            if(!rsa.VerifyData(Encoding.UTF8.GetBytes(payloadJson),Convert.FromBase64String(document.Signature),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1)){error="The activation key signature is invalid.";return false;}
            if(!string.Equals(document.Payload.Module,module,StringComparison.OrdinalIgnoreCase)){error=$"This key does not activate the {module} module.";return false;}
            if(document.Payload.ExpiresUtc.HasValue&&document.Payload.ExpiresUtc.Value.ToUniversalTime()<nowUtc){error="This module activation has expired.";return false;}
            if(!document.Payload.MachineIds.Contains(machineId,StringComparer.OrdinalIgnoreCase)){error=$"This PC is not included in the activation. Machine ID: {machineId}";return false;}
            payload=document.Payload;error=string.Empty;return true;
        }
        catch(FormatException){error="The activation key format is invalid.";return false;}
        catch(JsonException){error="The activation key data is invalid.";return false;}
        catch(CryptographicException){error="The activation key signature is invalid.";return false;}
    }

    private static string NormalizeBase64(string value)
    {
        var compact=new string(value.Where(character=>!char.IsWhiteSpace(character)).ToArray()).Replace('-','+').Replace('_','/');
        return compact.PadRight(compact.Length+(4-compact.Length%4)%4,'=');
    }
}
