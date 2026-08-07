using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface ICashierService
{
    Auction GetCurrentAuction();
    Invoice LoadInvoice(int bidderNumber);
    Invoice RecordPayments(int bidderNumber, IReadOnlyList<PaymentRequest> payments);
    Invoice RecordRefund(int bidderNumber, RefundRequest refund)=>throw new NotSupportedException();
    Invoice CorrectSale(int bidderNumber, SaleCorrectionRequest correction)=>throw new NotSupportedException();
    Invoice RegisterBuyer(AuctionBidder bidder);
    IReadOnlyList<RegisteredBidderSummary> GetRegisteredBidders();
    ConsignorSettlement LoadConsignorSettlement(int consignorNumber);
    ConsignorSettlement RecordConsignorPayout(int consignorNumber, ConsignorPayoutRequest payout);
    ConsignorSettlement RegisterConsignor(Consignor consignor);
    IReadOnlyList<UnpaidInvoiceSummary> GetUnpaidInvoices();
    AuctionSettlementReport GetAuctionSettlementReport();
    StaffPayment RecordStaffPayment(StaffPayment payment);
    StaffPayment VoidStaffPayment(long staffPaymentId,string reason,StaffPayment? replacement);
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);
}
