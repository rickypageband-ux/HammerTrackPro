namespace HammerTrackPro2.Models;

public sealed record SoldItem(long AuctionId, long SaleId, int LotNumber, string Description,
    int SellerNumber, decimal PriceEach, int QuantityPerDeal, AuctionSaleMode SaleMode);
