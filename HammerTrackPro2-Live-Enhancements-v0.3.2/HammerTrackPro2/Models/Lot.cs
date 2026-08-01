namespace HammerTrackPro2.Models;

public sealed class Lot
{
    public long AuctionId { get; set; }
    public long ConsignorId { get; set; }
    public int LotNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal StartingBid { get; set; }
    public decimal ReservePrice { get; set; }
    public bool PassOutMode { get; set; }
    public string Status { get; set; } = "Available";
    public string LotStatus { get; set; } = "Available";
}
