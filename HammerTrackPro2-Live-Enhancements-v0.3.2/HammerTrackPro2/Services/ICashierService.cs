using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface ICashierService
{
    Auction GetCurrentAuction();
    Invoice LoadInvoice(int bidderNumber);
    Invoice RecordPayments(int bidderNumber, IReadOnlyList<PaymentRequest> payments);
    Invoice RegisterBuyer(AuctionBidder bidder);
    ConsignorSettlement LoadConsignorSettlement(int consignorNumber);
    ConsignorSettlement RecordConsignorPayout(int consignorNumber, ConsignorPayoutRequest payout);
    ConsignorSettlement RegisterConsignor(Consignor consignor);
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);
}
