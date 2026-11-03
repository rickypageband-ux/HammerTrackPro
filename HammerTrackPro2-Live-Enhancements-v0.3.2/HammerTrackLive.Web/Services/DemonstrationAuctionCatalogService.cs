using HammerTrackLive.Web.Models;

namespace HammerTrackLive.Web.Services;

public sealed class DemonstrationAuctionCatalogService : IAuctionCatalogService
{
    private static readonly IReadOnlyList<AuctionSummary> Auctions =
    [
        new(
            "carolina-estate-and-tools",
            "Carolina Estate & Workshop Collection",
            "HammerTrack Demonstration Auction",
            "Columbia, South Carolina",
            new DateTimeOffset(2026, 11, 6, 8, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2026, 11, 14, 19, 0, 0, TimeSpan.FromHours(-5)),
            184,
            "Online Auction",
            "Preview",
            true,
            "/images/auction-tools.svg",
            "Quality shop equipment, hand tools, collectibles, and household furnishings."),
        new(
            "contractor-equipment-showcase",
            "Contractor Equipment Showcase",
            "HammerTrack Demonstration Auction",
            "Charlotte, North Carolina",
            new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.FromHours(-5)),
            96,
            "Live Video Auction",
            "Coming Soon",
            false,
            "/images/auction-equipment.svg",
            "A camera-enabled live auction demonstration with remote paddle bidding."),
        new(
            "holiday-collectibles",
            "Holiday Collectibles & Jewelry",
            "HammerTrack Demonstration Auction",
            "Online Nationwide",
            new DateTimeOffset(2026, 12, 1, 8, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2026, 12, 8, 20, 0, 0, TimeSpan.FromHours(-5)),
            240,
            "Online Auction",
            "Coming Soon",
            true,
            "/images/auction-collectibles.svg",
            "A timed online catalog demonstrating maximum bids, watchlists, and shipping filters.")
    ];

    public Task<IReadOnlyList<AuctionSummary>> GetFeaturedAuctionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Auctions);

    public Task<AuctionDetails?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var summary = Auctions.FirstOrDefault(auction => string.Equals(auction.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(summary is null ? null : new AuctionDetails(summary, 0, 0, "None", "Demonstration only", "Demonstration only", string.Empty, []));
    }
}
