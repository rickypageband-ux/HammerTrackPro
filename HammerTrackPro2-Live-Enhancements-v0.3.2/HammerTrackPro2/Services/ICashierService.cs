using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface ICashierService
{
    Auction GetCurrentAuction();
    Invoice LoadInvoice(int bidderNumber);
    Invoice RecordPayments(int bidderNumber, IReadOnlyList<PaymentRequest> payments);
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);
}
