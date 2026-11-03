namespace HammerTrackLive.Web.Data;

public sealed class OnlineAuctionTermsSnapshot
{
    public Guid AuctionId { get; set; }
    public OnlineAuction Auction { get; set; } = null!;
    public decimal BuyerPremiumPercent { get; set; }
    public decimal SalesTaxPercent { get; set; }
    public string TaxBasis { get; set; } = string.Empty;
    public string PaymentInstructions { get; set; } = string.Empty;
    public string PickupInstructions { get; set; } = string.Empty;
    public string ShippingInstructions { get; set; } = string.Empty;
    public DateTimeOffset CapturedUtc { get; set; }
}
