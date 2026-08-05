using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class CashierService : ICashierService
{
    private readonly IDataStore _dataStore;
    public CashierService(IDataStore dataStore) => _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
    public Auction GetCurrentAuction() => _dataStore.GetCurrentAuction();
    public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken=default) => Task.FromResult(true);
    public Invoice RegisterBuyer(AuctionBidder bidder){ArgumentNullException.ThrowIfNull(bidder);var auction=_dataStore.GetCurrentAuction();if(string.Equals(auction.Status,"Closed",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("This auction is closed. New buyers cannot be registered.");if(_dataStore.Load().Bidders.Any(x=>x.BidderNumber==bidder.BidderNumber))throw new InvalidOperationException($"Buyer Number #{bidder.BidderNumber} is already registered in the current auction. Use Lookup instead.");bidder.AuctionId=auction.AuctionId;new AuctionService(_dataStore).SaveBidder(bidder,null);return LoadInvoice(bidder.BidderNumber);}
    public IReadOnlyList<RegisteredBidderSummary> GetRegisteredBidders()=>_dataStore.Load().Bidders.OrderBy(x=>x.BidderNumber).Select(x=>new RegisteredBidderSummary{BuyerNumber=x.BidderNumber,Name=x.FullName}).ToList();
    public ConsignorSettlement RegisterConsignor(Consignor consignor)
    {
        ArgumentNullException.ThrowIfNull(consignor);
        if(string.Equals(_dataStore.GetCurrentAuction().Status,"Closed",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("This auction is closed. New sellers cannot be registered.");
        if(_dataStore.GetConsignors().Any(x=>x.ConsignorNumber==consignor.ConsignorNumber))throw new InvalidOperationException($"Seller Number #{consignor.ConsignorNumber} already exists. Use Lookup or Seller Management instead.");
        var existingBuyer=_dataStore.Load().Bidders.SingleOrDefault(x=>x.BidderNumber==consignor.ConsignorNumber);
        if(existingBuyer is not null)consignor.LinkedBuyerId=existingBuyer.BuyerId;
        consignor.Active=true;
        new ConsignorService(_dataStore).SaveConsignor(consignor,null);
        return LoadConsignorSettlement(consignor.ConsignorNumber);
    }
    public IReadOnlyList<UnpaidInvoiceSummary> GetUnpaidInvoices()=>_dataStore.Load().Bidders.Select(x=>LoadInvoice(x.BidderNumber)).Where(x=>x.BalanceDue>0).OrderBy(x=>x.BidderNumber).Select(x=>new UnpaidInvoiceSummary{BuyerNumber=x.BidderNumber,BuyerName=x.BuyerName,InvoiceTotal=x.TotalAmount,AmountPaid=x.AmountPaid,BalanceDue=x.BalanceDue}).ToList();
    public AuctionSettlementReport GetAuctionSettlementReport()=>_dataStore.LoadAuctionSettlementReport();
    public StaffPayment RecordStaffPayment(StaffPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);if(!new[]{"Clerk","Auctioneer","Floor Help","Kitchen Help"}.Contains(payment.Category))throw new InvalidOperationException("Select Clerk, Auctioneer, Floor Help, or Kitchen Help.");if(string.IsNullOrWhiteSpace(payment.Payee))throw new InvalidOperationException("Enter the staff payee name.");if(payment.Amount<=0)throw new InvalidOperationException("Staff payment amount must be greater than zero.");return _dataStore.RecordStaffPayment(payment);
    }
    public StaffPayment VoidStaffPayment(long id,string reason,StaffPayment? replacement){if(id<=0)throw new InvalidOperationException("Select a staff payment.");if(string.IsNullOrWhiteSpace(reason))throw new InvalidOperationException("A void reason is required.");if(replacement is not null){if(string.IsNullOrWhiteSpace(replacement.Payee)||replacement.Amount<=0)throw new InvalidOperationException("Replacement payee and positive amount are required.");replacement.ReplacementForStaffPaymentId=id;}return _dataStore.VoidStaffPayment(id,reason,replacement);}

    public ConsignorSettlement LoadConsignorSettlement(int consignorNumber)
    {
        if(consignorNumber<=0) throw new InvalidOperationException("Enter a valid seller number.");
        var settlement=_dataStore.LoadConsignorSettlement(consignorNumber);
        Recalculate(settlement);
        return settlement;
    }

    public ConsignorSettlement RecordConsignorPayout(int consignorNumber,ConsignorPayoutRequest payout)
    {
        ArgumentNullException.ThrowIfNull(payout);
        var settlement=LoadConsignorSettlement(consignorNumber);
        if(settlement.BalanceDue<=0) throw new InvalidOperationException("This seller settlement has no balance available to pay.");
        if(payout.Amount<=0) throw new InvalidOperationException("The payout amount must be greater than zero.");
        if(payout.Amount>settlement.BalanceDue) throw new InvalidOperationException("A seller payout cannot exceed the currently available settlement balance.");
        if(payout.Method==ConsignorPayoutMethod.Check&&settlement.HoldCheck)
            throw new InvalidOperationException("This seller is marked Hold Check. Clear Hold Check before issuing a check.");
        if(payout.Method is ConsignorPayoutMethod.Check or ConsignorPayoutMethod.Ach && string.IsNullOrWhiteSpace(payout.Reference))
            throw new InvalidOperationException($"Enter a reference for the {payout.Method} payout.");
        var updated=_dataStore.RecordConsignorPayout(settlement,new ConsignorPayout
        {
            Method=payout.Method,Amount=Money(payout.Amount),Reference=payout.Reference?.Trim()??string.Empty
        });
        Recalculate(updated);
        return updated;
    }

    public Invoice LoadInvoice(int bidderNumber)
    {
        if (bidderNumber <= 0) throw new InvalidOperationException("Enter a valid bidder number.");
        var invoice = _dataStore.LoadCashierInvoice(bidderNumber);
        Recalculate(invoice);
        return invoice;
    }

    public Invoice RecordPayments(int bidderNumber, IReadOnlyList<PaymentRequest> payments)
    {
        ArgumentNullException.ThrowIfNull(payments);
        if (string.Equals(_dataStore.GetCurrentAuction().Status, "Closed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This auction is closed. Payments are locked; invoices may still be viewed and reprinted.");
        if (payments.Count == 0) throw new InvalidOperationException("Enter at least one payment.");

        var invoice = LoadInvoice(bidderNumber);
        if (invoice.BalanceDue <= 0) throw new InvalidOperationException("This invoice is already paid.");
        var remaining = invoice.BalanceDue;var auction=_dataStore.GetCurrentAuction();
        var records = new List<Payment>();
        foreach (var request in payments)
        {
            if (request.Amount <= 0) throw new InvalidOperationException("Payment amounts must be greater than zero.");
            if (remaining <= 0) throw new InvalidOperationException("The entered payments exceed the invoice balance.");
            if (request.Method != PaymentMethod.Cash && request.Amount > remaining)
                throw new InvalidOperationException($"{request.Method} payments cannot exceed the remaining balance.");
            var fee=request.Method==PaymentMethod.Card?Money(request.Amount*auction.CreditCardFeePercent/100m+auction.CreditCardFeeFixedAmount):0m;var applied=Math.Min(request.Amount,remaining)+fee;
            records.Add(new Payment
            {
                Method=request.Method,TenderedAmount=request.Amount+fee,AppliedAmount=applied,ProcessingFee=fee,
                ChangeDue=request.Method == PaymentMethod.Cash ? request.Amount-applied : 0,
                Reference=request.Reference?.Trim() ?? string.Empty
            });
            remaining -= request.Amount;invoice.CreditCardFeeAmount+=fee;
        }
        invoice.TotalAmount+=records.Sum(item=>item.ProcessingFee);invoice.AmountPaid += records.Sum(item => item.AppliedAmount);
        invoice.BalanceDue = Math.Max(0, invoice.TotalAmount-invoice.AmountPaid);
        invoice.Status = invoice.BalanceDue == 0 ? "Paid" : invoice.AmountPaid > 0 ? "Partially Paid" : "Unpaid";
        var updated=_dataStore.RecordInvoicePayments(invoice,records);
        Recalculate(updated);
        return updated;
    }

    internal static void Recalculate(Invoice invoice)
    {
        invoice.HammerSubtotal = Money(invoice.Purchases.Sum(item => item.HammerPrice));
        invoice.BuyerPremiumAmount = Money(invoice.HammerSubtotal * invoice.BuyerPremiumPercent / 100m);
        var taxable = invoice.SalesTaxBasis switch
        {
            SalesTaxBasis.None => 0m,
            SalesTaxBasis.HammerOnly => invoice.HammerSubtotal,
            SalesTaxBasis.HammerPlusBuyerPremium => invoice.HammerSubtotal + invoice.BuyerPremiumAmount,
            _ => throw new InvalidOperationException("The auction sales-tax basis is invalid.")
        };
        invoice.SalesTaxAmount = Money(taxable * invoice.SalesTaxPercent / 100m);
        invoice.CreditCardFeeAmount=Money(invoice.Payments.Sum(item=>item.ProcessingFee));
        invoice.TotalAmount = invoice.HammerSubtotal + invoice.BuyerPremiumAmount + invoice.SalesTaxAmount+invoice.CreditCardFeeAmount;
        invoice.AmountPaid = Money(invoice.Payments.Sum(item => item.AppliedAmount));
        invoice.BalanceDue = Math.Max(0, invoice.TotalAmount-invoice.AmountPaid);
        invoice.Status = invoice.BalanceDue == 0 ? "Paid" : invoice.AmountPaid > 0 ? "Partially Paid" : "Unpaid";
    }

    internal static void Recalculate(ConsignorSettlement settlement)
    {
        settlement.HammerSubtotal=Money(settlement.Lots.Sum(item=>item.HammerPrice));
        settlement.CommissionAmount=Money(settlement.HammerSubtotal*settlement.CommissionPercent/100m);
        settlement.NetAmount=Math.Max(0,settlement.HammerSubtotal-settlement.CommissionAmount-settlement.BuyerPurchaseBalanceDeduction);
        settlement.AmountPaid=Money(settlement.Payouts.Sum(item=>item.Amount));
        settlement.BalanceDue=Math.Max(0,settlement.NetAmount-settlement.AmountPaid);
        settlement.Status=settlement.BalanceDue==0?"Paid":settlement.AmountPaid>0?"Partially Paid":"Unpaid";
    }

    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
