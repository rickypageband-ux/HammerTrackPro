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
    public IReadOnlyList<Buyer> GetBuyers() => _dataStore.GetBuyers();
    public DashboardSnapshot GetDashboardSnapshot() => _dataStore.GetDashboardSnapshot();
    public string GetSetting(string key, string defaultValue = "") => _dataStore.GetSetting(key, defaultValue);
    public void SaveSettings(IReadOnlyDictionary<string, string> settings)
    {
        if (settings.Keys.Any(key => key.StartsWith("LotNotes:", StringComparison.OrdinalIgnoreCase))) EnsureCurrentAuctionOpen();
        _dataStore.SaveSettings(settings);
    }
    public string Backup() => _dataStore.Backup();
    public string DataFilePath => _dataStore.DataFilePath;

    public void SaveBidder(AuctionBidder bidder, int? originalBidderNumber)
    {
        EnsureCurrentAuctionOpen();
        ValidateBidder(bidder);
        _dataStore.SaveBidder(bidder, originalBidderNumber);
    }

    public void DeleteBidder(int bidderNumber)
    {
        EnsureCurrentAuctionOpen();
        if (bidderNumber <= 0) throw new ArgumentOutOfRangeException(nameof(bidderNumber));
        _dataStore.DeleteBidder(bidderNumber);
    }

    public void SaveLot(AuctionLot lot, int? originalLotNumber)
    {
        EnsureCurrentAuctionOpen();
        ValidateLot(lot);
        _dataStore.SaveLot(lot, originalLotNumber);
    }

    public void DeleteLot(int lotNumber)
    {
        EnsureCurrentAuctionOpen();
        if (lotNumber <= 0) throw new ArgumentOutOfRangeException(nameof(lotNumber));
        _dataStore.DeleteLot(lotNumber);
    }
    public IReadOnlyList<Auction> GetAuctions() => _dataStore.GetAuctions();
    public Auction GetCurrentAuction() => _dataStore.GetCurrentAuction();
    public Auction SaveAuction(Auction auction)
    {
        ArgumentNullException.ThrowIfNull(auction);
        if (string.IsNullOrWhiteSpace(auction.AuctionNumber)) throw new InvalidOperationException("Auction number is required.");
        if (string.IsNullOrWhiteSpace(auction.AuctionName)) throw new InvalidOperationException("Auction name is required.");
        if (auction.BuyerPremiumPercent is < 0 or > 100 || auction.CreditCardFeePercent is < 0 or > 100 || auction.DefaultConsignorCommissionPercent is < 0 or > 100 || auction.SalesTaxPercent is < 0 or > 100) throw new InvalidOperationException("Auction percentages must be between 0 and 100.");
        if(auction.CreditCardFeeFixedAmount<0)throw new InvalidOperationException("The credit-card fixed fee cannot be negative.");
        if (auction.AuctionId > 0 && string.Equals(_dataStore.GetAuctions().Single(item => item.AuctionId == auction.AuctionId).Status, "Closed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Closed auctions cannot be edited.");
        return _dataStore.SaveAuction(auction);
    }

    public void SelectAuction(long auctionId)
    {
        if (auctionId <= 0) throw new ArgumentOutOfRangeException(nameof(auctionId));
        _dataStore.SelectAuction(auctionId);
    }

    public void CloseAuction(long auctionId)
    {
        if (auctionId <= 0) throw new ArgumentOutOfRangeException(nameof(auctionId));
        _dataStore.CloseAuction(auctionId);
    }

    public void CopyConsignorAssociations(long sourceAuctionId, long destinationAuctionId)
    {
        if (sourceAuctionId <= 0) throw new ArgumentOutOfRangeException(nameof(sourceAuctionId));
        if (destinationAuctionId <= 0) throw new ArgumentOutOfRangeException(nameof(destinationAuctionId));
        _dataStore.CopyConsignorAssociations(sourceAuctionId, destinationAuctionId);
    }

    public void ReopenAuction(long auctionId) =>
        throw new NotSupportedException("Reopening an auction requires administrator authentication and is not available in Sprint 2.");

    public Sale RecordSale(int lotNumber, int bidderNumber, decimal hammerPrice, bool keepLotOpen = false, int quantity = 1)
    {
        EnsureCurrentAuctionOpen();
        if (lotNumber <= 0) throw new InvalidOperationException("Enter a valid lot number.");
        if (bidderNumber <= 0) throw new InvalidOperationException("Enter a valid bidder number.");
        if (hammerPrice <= 0) throw new InvalidOperationException("Hammer price must be greater than zero.");
        if (quantity <= 0) throw new InvalidOperationException("Quantity must be at least one.");

        var data = _dataStore.Load();
        var lot = data.Lots.FirstOrDefault(item => item.LotNumber == lotNumber)
            ?? throw new InvalidOperationException($"Lot #{lotNumber} was not found.");
        if (string.Equals(lot.Status, "Sold", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Lot #{lotNumber} is closed and has already been sold.");
        if (!data.Bidders.Any(item => item.BidderNumber == bidderNumber))
            throw new InvalidOperationException($"Bidder #{bidderNumber} was not found.");

        return _dataStore.RecordSale(lotNumber, bidderNumber, hammerPrice, keepLotOpen, quantity);
    }

    public void CloseLot(int lotNumber)
    {
        EnsureCurrentAuctionOpen();
        if (lotNumber <= 0) throw new InvalidOperationException("Enter a valid lot number.");
        _dataStore.CloseLot(lotNumber);
    }

    public void PassLot(int lotNumber)
    {
        EnsureCurrentAuctionOpen();
        if (lotNumber <= 0) throw new InvalidOperationException("Enter a valid lot number.");
        if (_dataStore.GetSaleCountForLot(lotNumber) > 0)
            throw new InvalidOperationException($"Lot #{lotNumber} already has buyer entries. Close the lot instead of passing it.");
        _dataStore.PassLot(lotNumber);
    }

    public Sale? GetLastSale() => _dataStore.GetLastSale();
    public IReadOnlyList<Sale> GetSalesForLot(int lotNumber) => lotNumber <= 0 ? [] : _dataStore.GetSalesForLot(lotNumber);
    public int GetSaleCountForLot(int lotNumber) => lotNumber <= 0 ? 0 : _dataStore.GetSaleCountForLot(lotNumber);

    public void UndoLastSale(long saleId)
    {
        EnsureCurrentAuctionOpen();
        if (saleId <= 0) throw new ArgumentOutOfRangeException(nameof(saleId));
        _dataStore.UndoLastSale(saleId);
    }

    private void EnsureCurrentAuctionOpen()
    {
        if (string.Equals(_dataStore.GetCurrentAuction().Status, "Closed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This auction is closed. Operational changes are locked.");
    }

    private static void ValidateBidder(AuctionBidder bidder)
    {
        ArgumentNullException.ThrowIfNull(bidder);
        if (bidder.BidderNumber <= 0) throw new InvalidOperationException("Bidder number must be greater than zero.");
        if (bidder.AuctionId <= 0) throw new InvalidOperationException("Select an auction before registering a bidder.");
    }

    private static void ValidateLot(AuctionLot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);
        if (lot.LotNumber <= 0) throw new InvalidOperationException("Lot number must be greater than zero.");
        if (string.IsNullOrWhiteSpace(lot.Description)) throw new InvalidOperationException("A lot description is required.");
        if (lot.StartingBid < 0) throw new InvalidOperationException("Starting bid cannot be negative.");
        if (lot.AuctionId <= 0) throw new InvalidOperationException("Select an auction.");
        if (lot.ConsignorId <= 0) throw new InvalidOperationException("Select a seller.");
        if (lot.ReservePrice < 0) throw new InvalidOperationException("Reserve price cannot be negative.");
    }
}
