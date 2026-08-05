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
    [Category("Auction Company"), DisplayName("Company Name")]
    public string CompanyName { get; set; } = string.Empty;
    [Category("Auction Company"), DisplayName("Street Address")]
    public string CompanyAddress { get; set; } = string.Empty;
    [Category("Auction Company")]
    public string CompanyCity { get; set; } = string.Empty;
    [Category("Auction Company")]
    public string CompanyState { get; set; } = string.Empty;
    [Category("Auction Company"), DisplayName("ZIP Code")]
    public string CompanyZip { get; set; } = string.Empty;
    [Category("Auction Company"), DisplayName("Phone Number")]
    public string CompanyPhone { get; set; } = string.Empty;
    [Category("Auction Company"), DisplayName("Email Address")]
    public string CompanyEmail { get; set; } = string.Empty;
    [Category("Staff")]
    public string Auctioneer { get; set; } = string.Empty;
    [Category("Staff")]
    public string Clerk { get; set; } = string.Empty;
    [Category("Staff")]
    public string Cashier { get; set; } = string.Empty;
    [Category("Commercial Terms"), DisplayName("Buyer's Premium %")]
    public decimal BuyerPremiumPercent { get; set; }
    [Category("Commercial Terms"), DisplayName("Credit Card Fee %")]
    public decimal CreditCardFeePercent { get; set; } = 2.65m;
    [Category("Commercial Terms"), DisplayName("Credit Card Fixed Fee")]
    public decimal CreditCardFeeFixedAmount { get; set; } = 0.15m;
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

    [Browsable(false)]
    public string CompanyDisplayName => string.IsNullOrWhiteSpace(CompanyName)?AuctionName:CompanyName;

    [Browsable(false)]
    public string CompanyFormattedAddress => string.Join(", ",new[]{CompanyAddress,string.Join(" ",new[]{CompanyCity,CompanyState,CompanyZip}.Where(value=>!string.IsNullOrWhiteSpace(value)))}.Where(value=>!string.IsNullOrWhiteSpace(value)));
}

public enum SalesTaxBasis { None, HammerOnly, HammerPlusBuyerPremium }
