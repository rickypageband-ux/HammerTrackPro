namespace HammerTrackPro2.Models;

public sealed class AppData
{
    public List<AuctionBidder> Bidders { get; set; } = new();
    public List<AuctionLot> Lots { get; set; } = new();
}
