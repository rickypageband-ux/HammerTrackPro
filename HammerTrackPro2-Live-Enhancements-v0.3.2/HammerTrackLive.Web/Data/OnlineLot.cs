namespace HammerTrackLive.Web.Data;

public sealed class OnlineLot
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public OnlineAuction Auction { get; set; } = null!;
    public int LotNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal StartingBid { get; set; }
    public decimal BidIncrement { get; set; }
    public decimal? ReservePrice { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<OnlineLotMedia> Media { get; } = [];
}
