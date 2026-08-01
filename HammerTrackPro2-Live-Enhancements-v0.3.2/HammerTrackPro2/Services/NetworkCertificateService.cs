using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HammerTrackPro2.Services;

public static class NetworkCertificateService
{
    public static X509Certificate2 GetOrCreate(WorkstationConfiguration configuration)
    {
        using var store=new X509Store(StoreName.My,StoreLocation.CurrentUser); store.Open(OpenFlags.ReadWrite);
        if(!string.IsNullOrWhiteSpace(configuration.CertificateThumbprint))
        {
            var existing=store.Certificates.Find(X509FindType.FindByThumbprint,configuration.CertificateThumbprint,false)
                .OfType<X509Certificate2>().FirstOrDefault(item=>item.HasPrivateKey && item.NotAfter>DateTime.Now.AddDays(7));
            if(existing is not null) return existing;
        }
        using var key=RSA.Create(2048);
        var request=new CertificateRequest($"CN={Environment.MachineName}",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false,false,0,false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature|X509KeyUsageFlags.KeyEncipherment,false));
        var san=new SubjectAlternativeNameBuilder(); san.AddDnsName(Environment.MachineName); san.AddDnsName("localhost"); san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddYears(3));
        var persisted=new X509Certificate2(generated.Export(X509ContentType.Pfx),string.Empty,X509KeyStorageFlags.PersistKeySet|X509KeyStorageFlags.UserKeySet|X509KeyStorageFlags.Exportable);
        store.Add(persisted); configuration.CertificateThumbprint=persisted.Thumbprint; WorkstationConfigurationService.Save(configuration); return persisted;
    }
}
