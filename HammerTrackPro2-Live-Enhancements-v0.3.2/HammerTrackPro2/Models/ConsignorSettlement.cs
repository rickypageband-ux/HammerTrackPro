namespace HammerTrackPro2.Models;

public sealed class ConsignorSettlement
{
    public long ConsignorSettlementId { get; set; }
    public long AuctionId { get; set; }
    public long ConsignorId { get; set; }
    public string AuctionNumber { get; set; } = string.Empty;
    public int ConsignorNumber { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public string ConsignorAddress { get; set; } = string.Empty;
    public decimal CommissionPercent { get; set; }
    public decimal HammerSubtotal { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal BuyerPurchaseBalanceDeduction { get; set; }
    public decimal NetAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public bool HoldCheck { get; set; }
    public string Status { get; set; } = "Unpaid";
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public List<ConsignorSettlementLine> Lots { get; set; } = [];
    public List<ConsignorPayout> Payouts { get; set; } = [];
    public string FormattedSettlementNumber => $"{AuctionNumber}-C{ConsignorNumber:D4}";
}

public sealed class ConsignorSettlementLine
{
    public long SaleId { get; set; }
    public int LotNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal HammerPrice { get; set; }
    public decimal LineTotal => Quantity * HammerPrice;
}

public sealed class ConsignorPayout
{
    public long ConsignorPayoutId { get; set; }
    public long ConsignorSettlementId { get; set; }
    public ConsignorPayoutMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public sealed class ConsignorPayoutRequest
{
    public ConsignorPayoutMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public enum ConsignorPayoutMethod { Check, Cash, Ach }
