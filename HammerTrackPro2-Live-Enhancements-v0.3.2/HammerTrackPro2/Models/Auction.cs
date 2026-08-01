namespace HammerTrackPro2.Models;
public sealed class Auction
{
    public long AuctionId { get; set; }
    public string AuctionName { get; set; } = string.Empty;
    public DateTime AuctionDate { get; set; } = DateTime.Today;
    public string Location { get; set; } = string.Empty;
    public decimal BuyerPremiumPercent { get; set; }
    public decimal DefaultConsignorCommissionPercent { get; set; }
    public decimal SalesTaxPercent { get; set; }
    public string Status { get; set; } = "Setup";
}
