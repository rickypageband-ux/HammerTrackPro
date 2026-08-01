using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
namespace HammerTrackPro2.ViewModels;
public sealed class AuctionSetupViewModel
{
    private readonly IAuctionService _service;
    public Auction CurrentAuction { get; private set; } = new();
    public AuctionSetupViewModel(IAuctionService service) => _service = service;
    public void Load() => CurrentAuction = _service.GetCurrentAuction();
    public void Save()
    {
        if (string.IsNullOrWhiteSpace(CurrentAuction.AuctionName)) throw new InvalidOperationException("Auction name is required.");
        if (CurrentAuction.BuyerPremiumPercent is < 0 or > 100 || CurrentAuction.DefaultConsignorCommissionPercent is < 0 or > 100 || CurrentAuction.SalesTaxPercent is < 0 or > 100) throw new InvalidOperationException("Auction percentages must be between 0 and 100.");
        _service.SaveAuction(CurrentAuction);
    }
}
