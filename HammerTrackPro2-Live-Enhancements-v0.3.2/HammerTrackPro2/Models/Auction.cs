using System.ComponentModel;

namespace HammerTrackPro2.Models;

public sealed class Auction
{
    [ReadOnly(true), Category("Identity"), DisplayName("Auction ID")]
    public long AuctionId { get; set; }
    [Category("Identity"), DisplayName("Auction Number")]
    public string AuctionNumber { get; set; } = string.Empty;
    [Category("Identity"), DisplayName("Auction Name")]
    public string AuctionName { get; set; } = string.Empty;
    [Category("Schedule"), DisplayName("Auction Date")]
    public DateTime AuctionDate { get; set; } = DateTime.Today;
    [Category("Schedule"), DisplayName("Auction Time")]
    public TimeSpan AuctionTime { get; set; }
    [Category("Schedule")]
    public string Location { get; set; } = string.Empty;
    [Category("Staff")]
    public string Auctioneer { get; set; } = string.Empty;
    [Category("Staff")]
    public string Clerk { get; set; } = string.Empty;
    [Category("Staff")]
    public string Cashier { get; set; } = string.Empty;
    [Category("Commercial Terms"), DisplayName("Buyer's Premium %")]
    public decimal BuyerPremiumPercent { get; set; }
    [Category("Commercial Terms"), DisplayName("Credit Card Fee %")]
    public decimal CreditCardFeePercent { get; set; } = 3m;
    [Category("Commercial Terms"), DisplayName("Seller Commission %")]
    public decimal DefaultConsignorCommissionPercent { get; set; }
    [Category("Commercial Terms"), DisplayName("Sales Tax %")]
    public decimal SalesTaxPercent { get; set; }
    [Category("Commercial Terms"), DisplayName("Sales Tax Applies To")]
    public SalesTaxBasis SalesTaxBasis { get; set; } = SalesTaxBasis.HammerOnly;
    [Category("Printed Documents"), DisplayName("Invoice Footer")]
    public string InvoiceFooter { get; set; } = string.Empty;
    [Category("Printed Documents"), DisplayName("Receipt Footer")]
    public string ReceiptFooter { get; set; } = string.Empty;
    [Category("Identity"), DisplayName("Auction Status")]
    public string Status { get; set; } = "Setup";
    [ReadOnly(true), Category("Audit"), DisplayName("Created Date")]
    public DateTime CreatedDate { get; set; }

    [Browsable(false)]
    public string DisplayText => $"{AuctionNumber} - {AuctionName}";
}

public enum SalesTaxBasis { None, HammerOnly, HammerPlusBuyerPremium }
