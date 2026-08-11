using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class LiveAuctionViewModel
{
    private readonly IAuctionService _auctionService;
    private readonly ConsignorService _consignorService;

    public LiveAuctionViewModel(IAuctionService auctionService,ConsignorService consignorService)
    {
        _auctionService=auctionService??throw new ArgumentNullException(nameof(auctionService));
        _consignorService=consignorService??throw new ArgumentNullException(nameof(consignorService));
    }

    public AuctionLot QuickAddLot(int consignorNumber,string description,int currentLotNumber=0)
    {
        description=description?.Trim()??string.Empty;
        if(description.Length==0)throw new InvalidOperationException("Enter a lot description.");
        var consignor=_consignorService.GetByNumber(consignorNumber);
        if(!consignor.Active)throw new InvalidOperationException($"Seller #{consignorNumber} is inactive. Reactivate the seller before adding a new lot.");
        var auction=_auctionService.GetCurrentAuction();
        var lots=_auctionService.Load().Lots;
        var current=lots.FirstOrDefault(item=>item.LotNumber==currentLotNumber);
        if(current is not null&&!string.Equals(current.Status,"Sold",StringComparison.OrdinalIgnoreCase)&&!string.Equals(current.Status,"Passed",StringComparison.OrdinalIgnoreCase))
            _auctionService.CloseLot(currentLotNumber);
        var nextLotNumber=lots.Count==0?1:checked(lots.Max(item=>item.LotNumber)+1);
        var lot=new AuctionLot
        {
            AuctionId=auction.AuctionId,
            ConsignorId=consignor.ConsignorId,
            LotNumber=nextLotNumber,
            Description=description,
            Status="Available",
            LotStatus="Available"
        };
        _consignorService.AssociateWithCurrentAuction(consignorNumber);
        _auctionService.SaveLot(lot,null);
        return _auctionService.Load().Lots.Single(item=>item.LotNumber==nextLotNumber);
    }

    public void PassLot(int lotNumber)=>_auctionService.PassLot(lotNumber);

    public LiveAuctionEntryState GetEntryState(bool passOutMode)=>passOutMode
        ? new(false,false,false,false,true,true)
        : new(true,true,true,true,true,true);
}

public sealed record LiveAuctionEntryState(
    bool CanChangeSeller,
    bool CanChangeDescription,
    bool CanChangeLot,
    bool CanChangeHammerPrice,
    bool CanChangeBidder,
    bool CanChangeQuantity);
