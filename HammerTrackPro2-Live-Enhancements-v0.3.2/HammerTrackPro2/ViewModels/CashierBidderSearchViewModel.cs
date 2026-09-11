using HammerTrackPro2.Models;

namespace HammerTrackPro2.ViewModels;

public sealed class CashierBidderSearchViewModel
{
    private readonly IReadOnlyList<RegisteredBidderSummary> _bidders;
    public CashierBidderSearchViewModel(IReadOnlyList<RegisteredBidderSummary> bidders) => _bidders = bidders ?? throw new ArgumentNullException(nameof(bidders));

    public IReadOnlyList<RegisteredBidderSummary> Search(string? query)
    {
        var text=query?.Trim()??string.Empty;
        if(text.Length==0)return _bidders.OrderBy(x=>x.BuyerNumber).ToList();
        return _bidders.Where(x=>x.BuyerNumber.ToString().Contains(text,StringComparison.OrdinalIgnoreCase)
            ||x.Name.Contains(text,StringComparison.OrdinalIgnoreCase)
            ||x.DriversLicense.Contains(text,StringComparison.OrdinalIgnoreCase)).OrderBy(x=>x.BuyerNumber).ToList();
    }
}
