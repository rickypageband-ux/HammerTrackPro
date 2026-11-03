using HammerTrackLive.Web.Services;

namespace HammerTrackLive.Tests;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task FeaturedAuctions_AreClearlyDemonstrationContent()
    {
        var service = new DemonstrationAuctionCatalogService();

        var auctions = await service.GetFeaturedAuctionsAsync();

        Assert.NotEmpty(auctions);
        Assert.All(auctions, auction =>
        {
            Assert.Equal("HammerTrack Demonstration Auction", auction.AuctionCompany);
            Assert.True(auction.Status is "Preview" or "Coming Soon");
        });
    }

    [Fact]
    public async Task GetBySlug_IsCaseInsensitiveAndRejectsUnknownAuction()
    {
        var service = new DemonstrationAuctionCatalogService();

        Assert.NotNull(await service.GetBySlugAsync("CAROLINA-ESTATE-AND-TOOLS"));
        Assert.Null(await service.GetBySlugAsync("not-a-real-auction"));
    }
}
