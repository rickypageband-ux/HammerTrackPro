using HammerTrackPro2.Models;

namespace HammerTrackPro2.ViewModels;

public sealed class LiveSaleCorrectionViewModel
{
    private readonly LiveSaleHistoryItem _sale;

    public LiveSaleCorrectionViewModel(LiveSaleHistoryItem sale)=>_sale=sale??throw new ArgumentNullException(nameof(sale));

    public bool IsWholesale=>_sale.SaleMode==AuctionSaleMode.Wholesale;
    public int BidderNumber=>_sale.BidderNumber;
    public int QuantityPerDeal=>IsWholesale?_sale.QuantityPerDeal:_sale.Quantity;
    public int Deals=>IsWholesale?_sale.Deals:1;
    public decimal HammerPrice=>_sale.HammerPrice;
    public string Heading=>$"Lot #{_sale.LotNumber} — {_sale.Description}";
    public string Buyer=>$"Current buyer: {_sale.BuyerDisplay}";

    public SaleCorrectionRequest CreateRequest(int bidderNumber,int quantityPerDeal,int deals,decimal hammerPrice,string reason)
    {
        reason=reason?.Trim()??string.Empty;
        if(bidderNumber<=0)throw new InvalidOperationException("Enter a valid bidder number.");
        if(quantityPerDeal<=0)throw new InvalidOperationException("Quantity must be at least one.");
        if(deals<=0)throw new InvalidOperationException("Deals must be at least one.");
        if(hammerPrice<=0)throw new InvalidOperationException("Hammer price must be greater than zero.");
        if(reason.Length==0)throw new InvalidOperationException("Enter the reason for this correction.");
        var normalizedDeals=IsWholesale?deals:1;
        return new SaleCorrectionRequest
        {
            SaleId=_sale.SaleId,BidderNumber=bidderNumber,QuantityPerDeal=quantityPerDeal,Deals=normalizedDeals,
            Quantity=checked(quantityPerDeal*normalizedDeals),HammerPrice=hammerPrice,Reason=reason
        };
    }
}
