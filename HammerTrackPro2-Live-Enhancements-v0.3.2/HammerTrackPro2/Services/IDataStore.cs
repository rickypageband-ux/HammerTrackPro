using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface IDataStore : IConsignorRepository
{
    AppData Load();
    IReadOnlyList<Buyer> GetBuyers();
    Invoice LoadCashierInvoice(int bidderNumber);
    Invoice RecordInvoicePayments(Invoice invoice, IReadOnlyList<Payment> payments);
    ConsignorSettlement LoadConsignorSettlement(int consignorNumber);
    ConsignorSettlement RecordConsignorPayout(ConsignorSettlement settlement, ConsignorPayout payout);
    void SaveBidder(AuctionBidder bidder, int? originalBidderNumber);
    void DeleteBidder(int bidderNumber);
    void SaveLot(AuctionLot lot, int? originalLotNumber);
    void DeleteLot(int lotNumber);
    Auction GetCurrentAuction();
    IReadOnlyList<Auction> GetAuctions();
    Auction SaveAuction(Auction auction);
    void SelectAuction(long auctionId);
    void CloseAuction(long auctionId);
    void CopyConsignorAssociations(long sourceAuctionId, long destinationAuctionId);
    Sale RecordSale(int lotNumber, int bidderNumber, decimal hammerPrice, bool keepLotOpen = false);
    void CloseLot(int lotNumber);
    void PassLot(int lotNumber);
    Sale? GetLastSale();
    void UndoLastSale(long saleId);
    int GetSaleCountForLot(int lotNumber);
    DashboardSnapshot GetDashboardSnapshot();
    string GetSetting(string key, string defaultValue = "");
    void SaveSettings(IReadOnlyDictionary<string, string> settings);
    string Backup();
    string DataFilePath { get; }
    void ValidateSellerBuyerNumber(int sellerNumber, long? linkedBuyerId);
    void EnsureConsignorBuyerLink(int consignorNumber);
    AuctionSettlementReport LoadAuctionSettlementReport();
    StaffPayment RecordStaffPayment(StaffPayment payment);
    StaffPayment VoidStaffPayment(long staffPaymentId,string reason,StaffPayment? replacement);
}
