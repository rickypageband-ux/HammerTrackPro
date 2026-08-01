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
    public void AddPayment(PaymentMethod method, decimal amount, string reference)
    {
        if (amount <= 0) throw new InvalidOperationException("Payment amount must be greater than zero.");
        PendingPayments.Add(new PaymentRequest { Method=method,Amount=amount,Reference=reference?.Trim() ?? string.Empty });
    }
    public void RemovePayment(PaymentRequest payment) => PendingPayments.Remove(payment);
    public void RecordPayments()
    {
        if (Invoice is null) throw new InvalidOperationException("Look up a bidder before recording payments.");
        Invoice=_service.RecordPayments(Invoice.BidderNumber,PendingPayments); PendingPayments.Clear();
    }
    public void LookupConsignor(int consignorNumber) => Settlement=_service.LoadConsignorSettlement(consignorNumber);
    public void RecordConsignorPayout(ConsignorPayoutMethod method,decimal amount,string reference)
    {
        if(Settlement is null) throw new InvalidOperationException("Look up a consignor before recording a payout.");
        Settlement=_service.RecordConsignorPayout(Settlement.ConsignorNumber,new ConsignorPayoutRequest { Method=method,Amount=amount,Reference=reference?.Trim()??string.Empty });
    }
}
