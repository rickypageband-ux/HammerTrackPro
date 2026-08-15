namespace HammerTrackPro2.Tests;

public sealed class InstallerAgreementTests
{
    [Fact]
    public void InstallerUsesApprovedSoftwareLicenseAndOwnershipTerms()
    {
        var repository=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..",".."));
        var agreement=File.ReadAllText(Path.Combine(repository,"HammerTrackPro2.Installer","Eula.rtf"));
        Assert.Contains("HAMMER TRACK PRO SOFTWARE LICENSE AND OWNERSHIP TERMS",agreement);
        Assert.Contains("perpetual, limited, non-exclusive, non-transferable license",agreement);
        Assert.Contains("two (2) computers",agreement);
        Assert.Contains("One (1) Clerking PC",agreement);
        Assert.Contains("One (1) Cashier PC",agreement);
        Assert.Contains("HAMMER TRACK PRO IS LICENSED, NOT SOLD.",agreement);
        Assert.DoesNotContain("End User License Agreement (EULA)",agreement);

        var package=File.ReadAllText(Path.Combine(repository,"HammerTrackPro2.Installer","Package.wxs"));
        Assert.Contains("WixUILicenseRtf",package);Assert.Contains("Eula.rtf",package);
    }
}
