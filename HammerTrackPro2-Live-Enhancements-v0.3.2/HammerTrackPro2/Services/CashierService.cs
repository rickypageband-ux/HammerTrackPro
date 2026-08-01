using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class CashierService : ICashierService
{
    private readonly IDataStore _dataStore;
    public CashierService(IDataStore dataStore) => _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));

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
        var remaining = invoice.BalanceDue;
        var records = new List<Payment>();
        foreach (var request in payments)
        {
            if (request.Amount <= 0) throw new InvalidOperationException("Payment amounts must be greater than zero.");
            if (remaining <= 0) throw new InvalidOperationException("The entered payments exceed the invoice balance.");
            if (request.Method != PaymentMethod.Cash && request.Amount > remaining)
                throw new InvalidOperationException($"{request.Method} payments cannot exceed the remaining balance.");
            var applied = Math.Min(request.Amount, remaining);
            records.Add(new Payment
            {
                Method=request.Method,TenderedAmount=request.Amount,AppliedAmount=applied,
                ChangeDue=request.Method == PaymentMethod.Cash ? request.Amount-applied : 0,
                Reference=request.Reference?.Trim() ?? string.Empty
            });
            remaining -= applied;
        }
        invoice.AmountPaid += records.Sum(item => item.AppliedAmount);
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
        invoice.TotalAmount = invoice.HammerSubtotal + invoice.BuyerPremiumAmount + invoice.SalesTaxAmount;
        invoice.AmountPaid = Money(invoice.Payments.Sum(item => item.AppliedAmount));
        invoice.BalanceDue = Math.Max(0, invoice.TotalAmount-invoice.AmountPaid);
        invoice.Status = invoice.BalanceDue == 0 ? "Paid" : invoice.AmountPaid > 0 ? "Partially Paid" : "Unpaid";
    }

    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
