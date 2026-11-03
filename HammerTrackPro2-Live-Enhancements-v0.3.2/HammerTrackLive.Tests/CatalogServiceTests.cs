using System.Reflection;
using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Models;
using HammerTrackLive.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Tests;

public sealed class CatalogServiceTests
{
    [Fact]
    public void DatabaseCatalogQuery_OrdersBeforeProjectionAndTranslatesForPostgreSql()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=model_test;Password=model_test")
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new DatabaseAuctionCatalogService(context);
        var queryMethod = typeof(DatabaseAuctionCatalogService).GetMethod("Query", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var query = Assert.IsAssignableFrom<IQueryable<AuctionSummary>>(queryMethod.Invoke(service, null));
        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EndsUtc", sql, StringComparison.Ordinal);
    }

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
