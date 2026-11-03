using HammerTrackLive.Web.Data;

namespace HammerTrackLive.Web.Services;

public static class AuctionPublicationValidator
{
    public static IReadOnlyList<string> Validate(OnlineAuction auction)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(auction.Title)) errors.Add("Auction title is required.");
        if (string.IsNullOrWhiteSpace(auction.Description)) errors.Add("Auction description is required.");
        if (string.IsNullOrWhiteSpace(auction.Location)) errors.Add("Auction location is required.");
        if (auction.EndsUtc <= auction.StartsUtc) errors.Add("Auction closing time must be after its opening time.");
        if (auction.BuyerPremiumPercent is < 0 or > 100) errors.Add("Buyer premium must be between 0 and 100 percent.");
        if (auction.SalesTaxPercent is < 0 or > 100) errors.Add("Sales tax must be between 0 and 100 percent.");
        if (string.IsNullOrWhiteSpace(auction.PaymentInstructions)) errors.Add("Payment instructions are required.");
        if (string.IsNullOrWhiteSpace(auction.PickupInstructions)) errors.Add("Pickup instructions are required.");
        var activeLots = auction.Lots.Where(lot => lot.IsActive).ToList();
        if (activeLots.Count == 0) errors.Add("At least one active lot is required.");
        foreach (var lot in activeLots)
        {
            if (string.IsNullOrWhiteSpace(lot.Title)) errors.Add($"Lot {lot.LotNumber} needs a title.");
            if (lot.LotNumber <= 0) errors.Add("Every lot number must be positive.");
            if (lot.StartingBid < 0 || lot.BidIncrement <= 0) errors.Add($"Lot {lot.LotNumber} has invalid bidding amounts.");
            if (lot.Quantity <= 0) errors.Add($"Lot {lot.LotNumber} must have a positive quantity.");
        }
        return errors;
    }
}
