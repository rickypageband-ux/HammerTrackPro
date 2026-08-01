namespace HammerTrackPro2.Models;

public sealed class Sale
{
    public long Id { get; set; }
    public int LotNumber { get; set; }
    public int BidderNumber { get; set; }
    public decimal HammerPrice { get; set; }
    public DateTime SaleUtc { get; set; }
    public bool Paid { get; set; }

    public string DisplayText => $"Lot #{LotNumber} → Bidder #{BidderNumber} → {HammerPrice:C2}";
}
