namespace HammerTrackPro2.Models;

public sealed class LiveSaleHistoryItem
{
    public long SaleId { get; set; }
    public int LotNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public int BidderNumber { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityPerDeal { get; set; }
    public int Deals { get; set; }
    public decimal HammerPrice { get; set; }
    public DateTime SaleUtc { get; set; }
    public bool Paid { get; set; }
    public AuctionSaleMode SaleMode { get; set; }
    public decimal Total => Quantity * HammerPrice;
    public string BuyerDisplay => string.IsNullOrWhiteSpace(BuyerName) ? $"#{BidderNumber}" : $"#{BidderNumber} — {BuyerName}";
    public string LocalTime => SaleUtc.ToLocalTime().ToString("h:mm:ss tt");
}
