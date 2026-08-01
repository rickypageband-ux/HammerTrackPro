using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.Tests;

public sealed class ConsignorServiceTests
{
    [Fact]
    public void Consignor_DropdownTextUsesNumberAndDisplayName()
    {
        var consignor=new Consignor { ConsignorNumber=42,BusinessName="Smith Farms",Active=true };
        Assert.Equal("#42 — Smith Farms",consignor.ToString());
        consignor.Active=false;
        Assert.Equal("#42 — Smith Farms (Inactive)",consignor.ToString());
    }

    [Fact]
    public void SaveConsignor_CreatesUpdatesAndDeletesRecord()
    {
        var repository = new InMemoryConsignorRepository();
        var service = new ConsignorService(repository);
        var consignor = ValidConsignor();

        service.SaveConsignor(consignor, null);
        var created = Assert.Single(service.GetConsignors());
        Assert.Equal("Acme Farms", created.BusinessName);

        consignor.BusinessName = "Acme Estate";
        service.SaveConsignor(consignor, consignor.ConsignorNumber);
        Assert.Equal("Acme Estate", Assert.Single(service.GetConsignors()).BusinessName);

        service.DeleteConsignor(consignor.ConsignorNumber);
        Assert.Empty(service.GetConsignors());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void SaveConsignor_RejectsCommissionOutsideRange(decimal commission)
    {
        var consignor = ValidConsignor();
        consignor.DefaultCommissionPercent = commission == 0 ? -0.01m : commission;
        var service = new ConsignorService(new InMemoryConsignorRepository());

        Assert.Throws<InvalidOperationException>(() => service.SaveConsignor(consignor, null));
    }

    [Fact]
    public void SaveConsignor_RequiresBusinessOrContactName()
    {
        var consignor = ValidConsignor();
        consignor.BusinessName = string.Empty;
        var service = new ConsignorService(new InMemoryConsignorRepository());

        Assert.Throws<InvalidOperationException>(() => service.SaveConsignor(consignor, null));
    }

    private static Consignor ValidConsignor() => new()
    {
        ConsignorNumber = 12,
        BusinessName = "Acme Farms",
        DefaultCommissionPercent = 20,
        CommissionOverridePercent = 18,
        PrintCheck = true,
        Require1099 = true,
        Active = true
    };
}
