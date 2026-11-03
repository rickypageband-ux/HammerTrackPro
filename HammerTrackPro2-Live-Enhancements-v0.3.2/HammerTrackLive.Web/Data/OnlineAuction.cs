namespace HammerTrackLive.Web.Data;

public sealed class OnlineAuction
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "America/New_York";
    public DateTimeOffset StartsUtc { get; set; }
    public DateTimeOffset EndsUtc { get; set; }
    public string Status { get; set; } = AuctionStatuses.Draft;
    public bool ShippingAvailable { get; set; }
    public decimal BuyerPremiumPercent { get; set; }
    public decimal SalesTaxPercent { get; set; }
    public string TaxBasis { get; set; } = "HammerPlusBuyerPremium";
    public string PaymentInstructions { get; set; } = string.Empty;
    public string PickupInstructions { get; set; } = string.Empty;
    public string ShippingInstructions { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedUtc { get; set; }
    public ICollection<OnlineLot> Lots { get; } = [];
    public OnlineAuctionTermsSnapshot? TermsSnapshot { get; set; }
}
