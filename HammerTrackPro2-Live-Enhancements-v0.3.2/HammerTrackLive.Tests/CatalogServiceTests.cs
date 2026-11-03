using HammerTrackLive.Web.Services;

namespace HammerTrackLive.Tests;

public sealed class CatalogServiceTests
{
    [Fact]
    public void FeaturedAuctions_AreClearlyDemonstrationContent()
    {
        var service = new DemonstrationAuctionCatalogService();

        var auctions = service.GetFeaturedAuctions();

        Assert.NotEmpty(auctions);
        Assert.All(auctions, auction =>
        {
            Assert.Equal("HammerTrack Demonstration Auction", auction.AuctionCompany);
            Assert.True(auction.Status is "Preview" or "Coming Soon");
        });
    }

    [Fact]
    public void GetBySlug_IsCaseInsensitiveAndRejectsUnknownAuction()
    {
        var service = new DemonstrationAuctionCatalogService();

        Assert.NotNull(service.GetBySlug("CAROLINA-ESTATE-AND-TOOLS"));
        Assert.Null(service.GetBySlug("not-a-real-auction"));
    }
}
