namespace HammerTrackPro2.Models;

public sealed class Invoice
{
    public long InvoiceId { get; set; }
    public long AuctionId { get; set; }
    public long AuctionBidderId { get; set; }
    public string AuctionNumber { get; set; } = string.Empty;
    public int BidderNumber { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public decimal BuyerPremiumPercent { get; set; }
    public decimal SalesTaxPercent { get; set; }
    public SalesTaxBasis SalesTaxBasis { get; set; }
    public decimal HammerSubtotal { get; set; }
    public decimal BuyerPremiumAmount { get; set; }
    public decimal SalesTaxAmount { get; set; }
    public decimal CreditCardFeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = "Unpaid";
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public List<InvoiceLine> Purchases { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public string FormattedInvoiceNumber => $"{AuctionNumber}-{BidderNumber:D4}";
}

public sealed class InvoiceLine
{
    public long SaleId { get; set; }
    public int LotNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal HammerPrice { get; set; }
}

public sealed class Payment
{
    public long PaymentId { get; set; }
    public long InvoiceId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal TenderedAmount { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal ChangeDue { get; set; }
    public decimal ProcessingFee { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public sealed class PaymentRequest
{
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal TotalTender => Amount + ProcessingFee;
    public string Reference { get; set; } = string.Empty;
}

public enum PaymentMethod { Cash, Check, Card }
