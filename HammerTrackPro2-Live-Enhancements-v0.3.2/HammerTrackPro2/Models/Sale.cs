namespace HammerTrackPro2.Models;

public sealed class Sale
{
    public long Id { get; set; }
    public long AuctionId { get; set; }
    public int LotNumber { get; set; }
    public int BidderNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal HammerPrice { get; set; }
    public decimal TotalPrice => Quantity * HammerPrice;
    public DateTime SaleUtc { get; set; }
    public bool Paid { get; set; }

    public string DisplayText => $"Lot #{LotNumber} → Bidder #{BidderNumber} → {Quantity} × {HammerPrice:C2} = {TotalPrice:C2}";
}
