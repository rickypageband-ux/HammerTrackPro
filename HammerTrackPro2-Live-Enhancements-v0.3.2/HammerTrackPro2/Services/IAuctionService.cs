using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface IAuctionService : IAuctionSetupService
{
    AppData Load();
    IReadOnlyList<Buyer> GetBuyers();
    void SaveBidder(AuctionBidder bidder, int? originalBidderNumber);
    void DeleteBidder(int bidderNumber);
    void SaveLot(AuctionLot lot, int? originalLotNumber);
    void DeleteLot(int lotNumber);
    Sale RecordSale(int lotNumber, int bidderNumber, decimal hammerPrice, bool keepLotOpen = false, int quantity = 1, int deals = 1);
    void CloseLot(int lotNumber);
    void PassLot(int lotNumber);
    Sale? GetLastSale();
    IReadOnlyList<Sale> GetSalesForLot(int lotNumber);
    IReadOnlyList<LiveSaleHistoryItem> GetSaleHistory(AuctionSaleMode saleMode);
    void CorrectSale(int currentBidderNumber, SaleCorrectionRequest correction);
    void UndoLastSale(long saleId);
    int GetSaleCountForLot(int lotNumber);
    DashboardSnapshot GetDashboardSnapshot();
    string GetSetting(string key, string defaultValue = "");
    void SaveSettings(IReadOnlyDictionary<string, string> settings);
    string Backup();
    string DataFilePath { get; }
}
