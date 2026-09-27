using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class SoldItemSearchViewModel
{
    private readonly IAuctionService _service;
    public SoldItemSearchViewModel(IAuctionService service, AuctionSaleMode saleMode)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        SaleMode = saleMode;
    }

    public AuctionSaleMode SaleMode { get; }
    public SoldItem? SelectedItem { get; private set; }
    public IReadOnlyList<SoldItem> Results { get; private set; } = [];
    public int BidderNumber { get; set; }
    public int QuantityOrDeals { get; set; } = 1;
    public Sale? RecordedSale { get; private set; }
    public string CountLabel => SaleMode == AuctionSaleMode.Wholesale ? "Deals" : "Quantity";
    public decimal Total => SelectedItem is null ? 0 : checked(SelectedItem.PriceEach * QuantityOrDeals *
        (SaleMode == AuctionSaleMode.Wholesale ? SelectedItem.QuantityPerDeal : 1));

    public void Search(string query)
    {
        SelectedItem = null;
        Results = _service.SearchSoldItems(query, SaleMode);
    }

    public void Select(SoldItem? item)
    {
        if (item is not null && !Results.Contains(item)) throw new InvalidOperationException("Search for the item again.");
        SelectedItem = item;
        QuantityOrDeals = 1;
    }

    public Sale Record()
    {
        if (RecordedSale is not null) throw new InvalidOperationException("This purchase has already been recorded.");
        var item = SelectedItem ?? throw new InvalidOperationException("Select the item you want to sell.");
        RecordedSale = _service.RecordAdditionalBuyer(item.AuctionId, item.SaleId, SaleMode, BidderNumber, QuantityOrDeals);
        return RecordedSale;
    }
}
