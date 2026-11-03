using HammerTrackLive.Web.Models;

namespace HammerTrackLive.Web.Services;

public interface IAuctionCatalogService
{
    IReadOnlyList<AuctionSummary> GetFeaturedAuctions();

    AuctionSummary? GetBySlug(string slug);
}
