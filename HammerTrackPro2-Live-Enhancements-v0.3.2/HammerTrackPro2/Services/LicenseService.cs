using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace HammerTrackPro2.Services;

public sealed record LicensePayload(string LicenseId,string CustomerName,string[] MachineIds,DateTime IssuedUtc,DateTime? ExpiresUtc);
public sealed record LicenseDocument(LicensePayload Payload,string Signature);

public static class LicenseService
{
    internal const string PublicKeyPem="""
        -----BEGIN PUBLIC KEY-----
        MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEA4ug2lh+tJJK2VBOYbAo1
        zYmPbxgx3wacYVmYUW4holUJmAjCSF4qb9ne8g84wn3tKGuyGAZn1vspscgGcYvS
        wFut+bhFgRDmj8+JZRijD9ILPj081TPJddMCSccZfHzKd3wxkT18GXCRoHp4cu9M
        wIAYd5xHNICgJCST9MEd/bUXHKyfwyRtVcQo3iGQhy/JYgjNVTpcG0WDwxuGxE5U
        GRZf+vaDtBMEC6cDN1RFvw+iMdclHTEezorUopwP7KgyGUIWPTtp9In05tuMsL1P
        QDLrj7bxpi034nnIf29pX+ubMfb1VPVSD3piL7l7axRpVxa067cwc2luFt6kwYmR
        IfZt21XpPBLIfuSCCP0jKv5HOGrQ5QsPQKOA7YSPe8ui3T7KhDzvSknvF2hDEI/O
        3ZnrLiU/OY69lYxbDpHS/Y3mrUGKLBS+yxbbV4t362rczJF2++BJiycdqlebF25S
        K90nGakSNS4PMveyT4nshpAN40yCQzbb8LdpUef08OJBAgMBAAE=
        -----END PUBLIC KEY-----
        """;
    private static readonly JsonSerializerOptions JsonOptions=new() { WriteIndented=true,PropertyNameCaseInsensitive=true };
    public static string LicenseFile => Path.Combine(ApplicationDataLayoutService.SettingsFolder,"license.htplicense");

    public static string GetMachineId()
    {
        using var key=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        var machineGuid=key?.GetValue("MachineGuid")?.ToString();
        if(string.IsNullOrWhiteSpace(machineGuid))throw new InvalidOperationException("Windows did not provide a Machine ID.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("HammerTrackPro|"+machineGuid.ToUpperInvariant())))[..24];
    }

    public static bool TryLoadValidLicense(out LicensePayload? license,out string error)
    {
        license=null; ApplicationDataLayoutService.EnsureAndMigrateLegacyData();
        if(!File.Exists(LicenseFile)) { error="No HammerTrack Pro license is installed."; return false; }
        try { return ValidateDocument(File.ReadAllText(LicenseFile),GetMachineId(),PublicKeyPem,DateTime.UtcNow,out license,out error); }
        catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or JsonException or CryptographicException) { error="The installed license cannot be read or is invalid."; return false; }
    }

    public static LicensePayload InstallLicense(string sourceFile)
    {
        var json=File.ReadAllText(sourceFile);
        if(!ValidateDocument(json,GetMachineId(),PublicKeyPem,DateTime.UtcNow,out var license,out var error))throw new InvalidOperationException(error);
        Directory.CreateDirectory(ApplicationDataLayoutService.SettingsFolder); File.Copy(sourceFile,LicenseFile,true); return license!;
    }

    internal static bool ValidateDocument(string json,string machineId,string publicKeyPem,DateTime nowUtc,out LicensePayload? license,out string error)
    {
        license=null; var document=JsonSerializer.Deserialize<LicenseDocument>(json,JsonOptions);
        if(document?.Payload is null||string.IsNullOrWhiteSpace(document.Signature)) { error="The license file is incomplete."; return false; }
        var payloadJson=JsonSerializer.Serialize(document.Payload,JsonOptions); using var rsa=RSA.Create(); rsa.ImportFromPem(publicKeyPem);
        bool signatureValid;
        try { signatureValid=rsa.VerifyData(Encoding.UTF8.GetBytes(payloadJson),Convert.FromBase64String(document.Signature),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1); }
        catch(FormatException) { signatureValid=false; }
        if(!signatureValid) { error="The license signature is invalid."; return false; }
        if(document.Payload.ExpiresUtc.HasValue&&document.Payload.ExpiresUtc.Value.ToUniversalTime()<nowUtc) { error="This HammerTrack Pro license has expired."; return false; }
        if(!document.Payload.MachineIds.Contains(machineId,StringComparer.OrdinalIgnoreCase)) { error=$"This PC is not included in the license. Machine ID: {machineId}"; return false; }
        license=document.Payload; error=string.Empty; return true;
    }
}
