using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class BidderCardViewModel
{
    private readonly ICashierService _service;
    public BidderCardViewModel(ICashierService service){_service=service??throw new ArgumentNullException(nameof(service));Refresh();}
    public IReadOnlyList<RegisteredBidderSummary> RegisteredBidders { get; private set; }=[];
    public Auction CurrentAuction { get; private set; }=new();
    public int? SelectedBidderNumber { get; set; }
    public void Refresh(){CurrentAuction=_service.GetCurrentAuction();RegisteredBidders=_service.GetRegisteredBidders().OrderBy(x=>x.BuyerNumber).ToList();if(SelectedBidderNumber is null||RegisteredBidders.All(x=>x.BuyerNumber!=SelectedBidderNumber))SelectedBidderNumber=RegisteredBidders.FirstOrDefault()?.BuyerNumber;}
    public RegisteredBidderSummary GetSelectedBidder(){if(SelectedBidderNumber is null)throw new InvalidOperationException("Select a bidder number to print.");return RegisteredBidders.FirstOrDefault(x=>x.BuyerNumber==SelectedBidderNumber)??throw new InvalidOperationException($"Bidder #{SelectedBidderNumber} is not registered in the current auction.");}
}
