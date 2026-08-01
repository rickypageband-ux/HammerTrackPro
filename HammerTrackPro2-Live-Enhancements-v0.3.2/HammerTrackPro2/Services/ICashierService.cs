using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface ICashierService
{
    Invoice LoadInvoice(int bidderNumber);
    Invoice RecordPayments(int bidderNumber, IReadOnlyList<PaymentRequest> payments);
}
