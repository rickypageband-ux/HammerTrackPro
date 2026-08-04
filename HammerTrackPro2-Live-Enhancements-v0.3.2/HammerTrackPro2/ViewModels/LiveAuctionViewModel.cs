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

    public AuctionLot QuickAddLot(int consignorNumber,string description)
    {
        description=description?.Trim()??string.Empty;
        if(description.Length==0)throw new InvalidOperationException("Enter a lot description.");
        var consignor=_consignorService.GetByNumber(consignorNumber);
        if(!consignor.Active)throw new InvalidOperationException($"Consignor #{consignorNumber} is inactive. Reactivate the consignor before adding a new lot.");
        var auction=_auctionService.GetCurrentAuction();
        var lots=_auctionService.Load().Lots;
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
}
