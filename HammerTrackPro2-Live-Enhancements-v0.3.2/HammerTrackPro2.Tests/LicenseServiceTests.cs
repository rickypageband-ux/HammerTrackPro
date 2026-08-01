using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.Tests;

public sealed class LicenseServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions=new() { WriteIndented=true };

    [Fact]
    public void SignedLicense_AllowsOnlyListedMachines()
    {
        using var rsa=RSA.Create(2048); var document=CreateDocument(rsa,["PC-ONE"],null);
        Assert.True(LicenseService.ValidateDocument(document,"PC-ONE",rsa.ExportSubjectPublicKeyInfoPem(),DateTime.UtcNow,out var license,out var error)); Assert.NotNull(license); Assert.Empty(error);
        Assert.False(LicenseService.ValidateDocument(document,"PC-TWO",rsa.ExportSubjectPublicKeyInfoPem(),DateTime.UtcNow,out _,out error)); Assert.Contains("not included",error);
    }

    [Fact]
    public void License_RejectsTamperingAndExpiration()
    {
        using var rsa=RSA.Create(2048); var expired=CreateDocument(rsa,["PC-ONE"],DateTime.UtcNow.AddDays(-1));
        Assert.False(LicenseService.ValidateDocument(expired,"PC-ONE",rsa.ExportSubjectPublicKeyInfoPem(),DateTime.UtcNow,out _,out var error)); Assert.Contains("expired",error);
        var tampered=expired.Replace("Test Customer","Changed Customer",StringComparison.Ordinal);
        Assert.False(LicenseService.ValidateDocument(tampered,"PC-ONE",rsa.ExportSubjectPublicKeyInfoPem(),DateTime.UtcNow.AddDays(-2),out _,out error)); Assert.Contains("signature",error);
    }

    private static string CreateDocument(RSA rsa,string[] machines,DateTime? expires)
    {
        var payload=new LicensePayload(Guid.NewGuid().ToString(),"Test Customer",machines,DateTime.UtcNow,expires); var json=JsonSerializer.Serialize(payload,JsonOptions);
        var signature=rsa.SignData(Encoding.UTF8.GetBytes(json),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        return JsonSerializer.Serialize(new LicenseDocument(payload,Convert.ToBase64String(signature)),JsonOptions);
    }
}
