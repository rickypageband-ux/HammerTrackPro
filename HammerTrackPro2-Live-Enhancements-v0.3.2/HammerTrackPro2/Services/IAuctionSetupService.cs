using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public interface IAuctionSetupService
{
    IReadOnlyList<Auction> GetAuctions();
    Auction GetCurrentAuction();
    Auction SaveAuction(Auction auction);
    void SelectAuction(long auctionId);
    void CloseAuction(long auctionId);
    void CopyConsignorAssociations(long sourceAuctionId, long destinationAuctionId);
    void ReopenAuction(long auctionId);
}
