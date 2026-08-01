using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class AuctionService : IAuctionService
{
    private readonly IDataStore _dataStore;

    public AuctionService(IDataStore dataStore)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
    }

    public AppData Load() => _dataStore.Load();
    public DashboardSnapshot GetDashboardSnapshot() => _dataStore.GetDashboardSnapshot();
    public string GetSetting(string key, string defaultValue = "") => _dataStore.GetSetting(key, defaultValue);
    public void SaveSettings(IReadOnlyDictionary<string, string> settings) => _dataStore.SaveSettings(settings);
    public string Backup() => _dataStore.Backup();
    public string DataFilePath => _dataStore.DataFilePath;

    public void SaveBidder(Bidder bidder, int? originalBidderNumber)
    {
        ValidateBidder(bidder);
        _dataStore.SaveBidder(bidder, originalBidderNumber);
    }

    public void DeleteBidder(int bidderNumber)
    {
        if (bidderNumber <= 0) throw new ArgumentOutOfRangeException(nameof(bidderNumber));
        _dataStore.DeleteBidder(bidderNumber);
    }

    public void SaveLot(Lot lot, int? originalLotNumber)
    {
        ValidateLot(lot);
        _dataStore.SaveLot(lot, originalLotNumber);
    }

    public void DeleteLot(int lotNumber)
    {
        if (lotNumber <= 0) throw new ArgumentOutOfRangeException(nameof(lotNumber));
        _dataStore.DeleteLot(lotNumber);
    }
    public Auction GetCurrentAuction() => _dataStore.GetCurrentAuction();
    public void SaveAuction(Auction auction)
    {
        ArgumentNullException.ThrowIfNull(auction);
        if (string.IsNullOrWhiteSpace(auction.AuctionName)) throw new InvalidOperationException("Auction name is required.");
        if (auction.BuyerPremiumPercent is < 0 or > 100 || auction.DefaultConsignorCommissionPercent is < 0 or > 100 || auction.SalesTaxPercent is < 0 or > 100) throw new InvalidOperationException("Auction percentages must be between 0 and 100.");
        _dataStore.SaveAuction(auction);
    }

    public Sale RecordSale(int lotNumber, int bidderNumber, decimal hammerPrice, bool keepLotOpen = false)
    {
        if (lotNumber <= 0) throw new InvalidOperationException("Enter a valid lot number.");
        if (bidderNumber <= 0) throw new InvalidOperationException("Enter a valid bidder number.");
        if (hammerPrice <= 0) throw new InvalidOperationException("Hammer price must be greater than zero.");

        var data = _dataStore.Load();
        var lot = data.Lots.FirstOrDefault(item => item.LotNumber == lotNumber)
            ?? throw new InvalidOperationException($"Lot #{lotNumber} was not found.");
        if (string.Equals(lot.Status, "Sold", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Lot #{lotNumber} is closed and has already been sold.");
        if (!data.Bidders.Any(item => item.BidderNumber == bidderNumber))
            throw new InvalidOperationException($"Bidder #{bidderNumber} was not found.");

        return _dataStore.RecordSale(lotNumber, bidderNumber, hammerPrice, keepLotOpen);
    }

    public void CloseLot(int lotNumber)
    {
        if (lotNumber <= 0) throw new InvalidOperationException("Enter a valid lot number.");
        _dataStore.CloseLot(lotNumber);
    }

    public Sale? GetLastSale() => _dataStore.GetLastSale();
    public int GetSaleCountForLot(int lotNumber) => lotNumber <= 0 ? 0 : _dataStore.GetSaleCountForLot(lotNumber);

    public void UndoLastSale(long saleId)
    {
        if (saleId <= 0) throw new ArgumentOutOfRangeException(nameof(saleId));
        _dataStore.UndoLastSale(saleId);
    }

    private static void ValidateBidder(Bidder bidder)
    {
        ArgumentNullException.ThrowIfNull(bidder);
        if (bidder.BidderNumber <= 0) throw new InvalidOperationException("Bidder number must be greater than zero.");
        if (string.IsNullOrWhiteSpace(bidder.FirstName) && string.IsNullOrWhiteSpace(bidder.LastName))
            throw new InvalidOperationException("A bidder name is required.");
    }

    private static void ValidateLot(Lot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);
        if (lot.LotNumber <= 0) throw new InvalidOperationException("Lot number must be greater than zero.");
        if (string.IsNullOrWhiteSpace(lot.Description)) throw new InvalidOperationException("A lot description is required.");
        if (lot.StartingBid < 0) throw new InvalidOperationException("Starting bid cannot be negative.");
        if (lot.AuctionId <= 0) throw new InvalidOperationException("Select an auction.");
        if (lot.ConsignorId <= 0) throw new InvalidOperationException("Select a consignor.");
        if (lot.ReservePrice < 0) throw new InvalidOperationException("Reserve price cannot be negative.");
    }
}
