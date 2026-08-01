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
    Sale RecordSale(int lotNumber, int bidderNumber, decimal hammerPrice, bool keepLotOpen = false);
    void CloseLot(int lotNumber);
    Sale? GetLastSale();
    void UndoLastSale(long saleId);
    int GetSaleCountForLot(int lotNumber);
    DashboardSnapshot GetDashboardSnapshot();
    string GetSetting(string key, string defaultValue = "");
    void SaveSettings(IReadOnlyDictionary<string, string> settings);
    string Backup();
    string DataFilePath { get; }
}
