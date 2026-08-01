using System.ComponentModel;

namespace HammerTrackPro2.Models;

public sealed class Auction
{
    [ReadOnly(true)]
    public long AuctionId { get; set; }
    public string AuctionNumber { get; set; } = string.Empty;
    public string AuctionName { get; set; } = string.Empty;
    public DateTime AuctionDate { get; set; } = DateTime.Today;
    public TimeSpan AuctionTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Auctioneer { get; set; } = string.Empty;
    public string Clerk { get; set; } = string.Empty;
    public string Cashier { get; set; } = string.Empty;
    [DisplayName("Buyer's Premium %")]
    public decimal BuyerPremiumPercent { get; set; }
    [DisplayName("Seller Commission %")]
    public decimal DefaultConsignorCommissionPercent { get; set; }
    [DisplayName("Sales Tax %")]
    public decimal SalesTaxPercent { get; set; }
    [DisplayName("Sales Tax Applies To")]
    public SalesTaxBasis SalesTaxBasis { get; set; } = SalesTaxBasis.HammerOnly;
    public string InvoiceFooter { get; set; } = string.Empty;
    public string ReceiptFooter { get; set; } = string.Empty;
    [DisplayName("Auction Status")]
    public string Status { get; set; } = "Setup";
    [ReadOnly(true)]
    public DateTime CreatedDate { get; set; }

    public string DisplayText => $"{AuctionNumber} - {AuctionName}";
}

public enum SalesTaxBasis { None, HammerOnly, HammerPlusBuyerPremium }
