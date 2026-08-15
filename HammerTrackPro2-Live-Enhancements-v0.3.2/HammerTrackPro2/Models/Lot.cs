namespace HammerTrackPro2.Models;

public enum AuctionSaleMode
{
    Retail,
    Wholesale
}

public class AuctionLot
{
    public long AuctionLotId { get; set; }
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
    public AuctionSaleMode SaleMode { get; set; } = AuctionSaleMode.Retail;
}

[Obsolete("Use AuctionLot. Lot remains as a compatibility type for existing callers.")]
public sealed class Lot : AuctionLot
{
}
