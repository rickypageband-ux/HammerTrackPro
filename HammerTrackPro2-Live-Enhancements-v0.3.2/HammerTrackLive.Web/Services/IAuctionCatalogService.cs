using HammerTrackLive.Web.Models;

namespace HammerTrackLive.Web.Services;

public interface IAuctionCatalogService
{
    Task<IReadOnlyList<AuctionSummary>> GetFeaturedAuctionsAsync(CancellationToken cancellationToken = default);

    Task<AuctionDetails?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
