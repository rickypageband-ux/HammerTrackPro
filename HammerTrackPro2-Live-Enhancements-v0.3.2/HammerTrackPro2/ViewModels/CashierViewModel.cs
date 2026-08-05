using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class CashierViewModel
{
    private readonly ICashierService _service;
    public CashierViewModel(ICashierService service) => _service=service ?? throw new ArgumentNullException(nameof(service));
    public Invoice? Invoice { get; private set; }
    public ConsignorSettlement? Settlement { get; private set; }
    public List<PaymentRequest> PendingPayments { get; } = [];
    public void Lookup(int bidderNumber) { Invoice=_service.LoadInvoice(bidderNumber); PendingPayments.Clear(); }
    public void RegisterBuyer(AuctionBidder bidder){Invoice=_service.RegisterBuyer(bidder);PendingPayments.Clear();}
    public void AddPayment(PaymentMethod method, decimal amount, string reference)
    {
        if (amount <= 0) throw new InvalidOperationException("Payment amount must be greater than zero.");
        var remaining=RemainingInvoiceAmount;
        if(method!=PaymentMethod.Cash&&amount>remaining)throw new InvalidOperationException($"Enter no more than the remaining invoice amount of {remaining:C2}. The card fee is added automatically after this amount.");
        PendingPayments.Add(new PaymentRequest { Method=method,Amount=amount,ProcessingFee=CalculateCardFee(method,amount),Reference=reference?.Trim() ?? string.Empty });
    }
    public decimal CalculateCardFee(PaymentMethod method,decimal amount)
    {
        if(method!=PaymentMethod.Card||amount<=0)return 0m;
        var auction=_service.GetCurrentAuction();
        return decimal.Round(amount*auction.CreditCardFeePercent/100m+auction.CreditCardFeeFixedAmount,2,MidpointRounding.AwayFromZero);
    }
    public decimal RemainingInvoiceAmount=>Math.Max(0m,(Invoice?.BalanceDue??0m)-PendingPayments.Sum(item=>item.Amount));
    public decimal SuggestedPaymentAmount(PaymentMethod method)=>RemainingInvoiceAmount;
    public string CardFeeDescription
    {
        get{var auction=_service.GetCurrentAuction();return $"{auction.CreditCardFeePercent:0.##}% + {auction.CreditCardFeeFixedAmount:C2}";}
    }
    public decimal PendingCardFee=>PendingPayments.Sum(item=>item.ProcessingFee);
    public Invoice CreateInvoicePreview(PaymentMethod method,decimal amount)
    {
        var invoice=Invoice??throw new InvalidOperationException("Look up a bidder before printing.");
        var previewFee=PendingCardFee+CalculateCardFee(method,amount);
        return new Invoice
        {
            InvoiceId=invoice.InvoiceId,AuctionId=invoice.AuctionId,AuctionBidderId=invoice.AuctionBidderId,
            AuctionNumber=invoice.AuctionNumber,BidderNumber=invoice.BidderNumber,BuyerName=invoice.BuyerName,BuyerAddress=invoice.BuyerAddress,
            BuyerPremiumPercent=invoice.BuyerPremiumPercent,SalesTaxPercent=invoice.SalesTaxPercent,SalesTaxBasis=invoice.SalesTaxBasis,
            HammerSubtotal=invoice.HammerSubtotal,BuyerPremiumAmount=invoice.BuyerPremiumAmount,SalesTaxAmount=invoice.SalesTaxAmount,
            CreditCardFeeAmount=invoice.CreditCardFeeAmount+previewFee,TotalAmount=invoice.TotalAmount+previewFee,
            AmountPaid=invoice.AmountPaid,BalanceDue=invoice.BalanceDue+previewFee,Status=invoice.Status,
            CreatedUtc=invoice.CreatedUtc,UpdatedUtc=invoice.UpdatedUtc,Purchases=invoice.Purchases.ToList(),Payments=invoice.Payments.ToList()
        };
    }
    public void RemovePayment(PaymentRequest payment) => PendingPayments.Remove(payment);
    public void RecordPayments()
    {
        if (Invoice is null) throw new InvalidOperationException("Look up a bidder before recording payments.");
        Invoice=_service.RecordPayments(Invoice.BidderNumber,PendingPayments); PendingPayments.Clear();
    }
    public void LookupConsignor(int consignorNumber) => Settlement=_service.LoadConsignorSettlement(consignorNumber);
    public void RegisterConsignor(Consignor consignor)=>Settlement=_service.RegisterConsignor(consignor);
    public void RecordConsignorPayout(ConsignorPayoutMethod method,decimal amount,string reference)
    {
        if(Settlement is null) throw new InvalidOperationException("Look up a seller before recording a payout.");
        Settlement=_service.RecordConsignorPayout(Settlement.ConsignorNumber,new ConsignorPayoutRequest { Method=method,Amount=amount,Reference=reference?.Trim()??string.Empty });
    }
}
