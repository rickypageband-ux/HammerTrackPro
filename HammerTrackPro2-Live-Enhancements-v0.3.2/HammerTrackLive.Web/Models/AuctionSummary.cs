namespace HammerTrackLive.Web.Models;

public sealed record AuctionSummary(
    string Slug,
    string Title,
    string AuctionCompany,
    string Location,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int LotCount,
    string Format,
    string Status,
    bool ShippingAvailable,
    string ImagePath,
    string Description);
