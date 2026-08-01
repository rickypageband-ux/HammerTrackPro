namespace HammerTrackPro2.Models;

public sealed class AuctionBidder
{
    public long AuctionBidderId { get; set; }
    public long AuctionId { get; set; }
    public long BuyerId { get; set; }
    public int BidderNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DriversLicense { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
}
