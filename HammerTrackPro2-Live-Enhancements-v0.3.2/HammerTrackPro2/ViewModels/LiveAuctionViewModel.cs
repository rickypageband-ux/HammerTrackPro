using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class LiveAuctionViewModel
{
    private readonly IAuctionService _auctionService;
    private readonly ConsignorService _consignorService;

    public LiveAuctionViewModel(IAuctionService auctionService,ConsignorService consignorService,AuctionSaleMode saleMode=AuctionSaleMode.Retail)
    {
        _auctionService=auctionService??throw new ArgumentNullException(nameof(auctionService));
        _consignorService=consignorService??throw new ArgumentNullException(nameof(consignorService));
        SaleMode=saleMode;
    }

    public AuctionSaleMode SaleMode { get; }

    public IReadOnlyList<LiveSaleHistoryItem> GetSaleHistory()=>_auctionService.GetSaleHistory(SaleMode);

    public void CorrectSale(int currentBidderNumber,SaleCorrectionRequest correction)=>_auctionService.CorrectSale(currentBidderNumber,correction);

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
            ,SaleMode=SaleMode
        };
        _consignorService.AssociateWithCurrentAuction(consignorNumber);
        _auctionService.SaveLot(lot,null);
        return _auctionService.Load().Lots.Single(item=>item.LotNumber==nextLotNumber);
    }

    public void PassLot(int lotNumber)=>_auctionService.PassLot(lotNumber);

    public LiveAuctionEntryState GetEntryState(bool passOutMode)=>passOutMode
        ? new(false,false,false,false,true,SaleMode==AuctionSaleMode.Retail,SaleMode==AuctionSaleMode.Wholesale)
        : new(true,true,true,true,true,true,false);

    public WholesaleSaleCalculation CalculateWholesaleSale(decimal priceEach,int quantityPerDeal,int deals)
    {
        if(priceEach<=0)throw new InvalidOperationException("Hammer price must be greater than zero.");
        if(quantityPerDeal<=0)throw new InvalidOperationException("Quantity per deal must be at least one.");
        if(deals<=0)throw new InvalidOperationException("Deals must be at least one.");
        var totalQuantity=checked(quantityPerDeal*deals);
        return new(totalQuantity,priceEach*totalQuantity);
    }
}

public sealed record LiveAuctionEntryState(
    bool CanChangeSeller,
    bool CanChangeDescription,
    bool CanChangeLot,
    bool CanChangeHammerPrice,
    bool CanChangeBidder,
    bool CanChangeQuantity,
    bool CanChangeDeals);

public sealed record WholesaleSaleCalculation(int TotalQuantity,decimal SaleTotal);
