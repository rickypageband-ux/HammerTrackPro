namespace HammerTrackLive.Web.Models;

public sealed record AuctionDetails(
    AuctionSummary Summary,
    decimal BuyerPremiumPercent,
    decimal SalesTaxPercent,
    string TaxBasis,
    string PaymentInstructions,
    string PickupInstructions,
    string ShippingInstructions,
    IReadOnlyList<PublicLot> Lots);

public sealed record PublicLot(
    int LotNumber,
    string Title,
    string Description,
    string Category,
    string Condition,
    decimal StartingBid,
    decimal BidIncrement,
    int Quantity,
    IReadOnlyList<PublicMedia> Media);

public sealed record PublicMedia(Guid Id, string OriginalFileName, string Kind);
