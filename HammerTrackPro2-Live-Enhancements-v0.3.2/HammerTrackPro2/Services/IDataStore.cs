using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface IDataStore
{
    AppData Load();
    void SaveBidder(Bidder bidder, int? originalBidderNumber);
    void DeleteBidder(int bidderNumber);
    void SaveLot(Lot lot, int? originalLotNumber);
    void DeleteLot(int lotNumber);
    Auction GetCurrentAuction();
    void SaveAuction(Auction auction);
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
