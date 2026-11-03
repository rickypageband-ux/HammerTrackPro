using HammerTrackLive.Web.Services;

namespace HammerTrackLive.Tests;

public sealed class PricingCatalogTests
{
    [Fact]
    public void OnlinePlan_SeparatesPlatformAndProcessingCharges()
    {
        var online = Assert.Single(PricingCatalog.GetPlans(), plan => plan.Name == "HammerTrack Online");

        Assert.Equal("$100/month", online.Price);
        Assert.Contains(online.Features, feature => feature.Contains("2% of online hammer sales"));
        Assert.Contains(online.Features, feature => feature.Contains("processing billed separately"));
    }

    [Fact]
    public void LivePlan_DoesNotClaimIntegratedCheckout()
    {
        var live = Assert.Single(PricingCatalog.GetPlans(), plan => plan.Name == "HammerTrack Live");

        Assert.Contains(live.Features, feature => feature.Contains("Seller provides payment instructions"));
        Assert.Contains(live.Features, feature => feature.Contains("No percentage"));
    }
}
