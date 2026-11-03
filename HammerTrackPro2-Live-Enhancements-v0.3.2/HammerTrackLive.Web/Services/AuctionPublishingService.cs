using HammerTrackLive.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Services;

public sealed class AuctionPublishingService(ApplicationDbContext dbContext, TimeProvider timeProvider)
    : IAuctionPublishingService
{
    public async Task<AuctionPublishingResult> PublishAsync(Guid organizationId, Guid auctionId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var auction = await dbContext.OnlineAuctions
            .Include(item => item.Lots)
            .Include(item => item.TermsSnapshot)
            .SingleOrDefaultAsync(item => item.Id == auctionId && item.OrganizationId == organizationId, cancellationToken);
        if (auction is null) return AuctionPublishingResult.Failure(["The auction could not be found."]);
        if (auction.Status != AuctionStatuses.Draft) return AuctionPublishingResult.Failure(["Only a draft auction can be published."]);

        var errors = AuctionPublicationValidator.Validate(auction);
        if (errors.Count > 0) return AuctionPublishingResult.Failure(errors);

        var now = timeProvider.GetUtcNow();
        auction.TermsSnapshot = new OnlineAuctionTermsSnapshot
        {
            AuctionId = auction.Id,
            BuyerPremiumPercent = auction.BuyerPremiumPercent,
            SalesTaxPercent = auction.SalesTaxPercent,
            TaxBasis = auction.TaxBasis,
            PaymentInstructions = auction.PaymentInstructions,
            PickupInstructions = auction.PickupInstructions,
            ShippingInstructions = auction.ShippingInstructions,
            CapturedUtc = now
        };
        auction.Status = AuctionStatuses.Published;
        auction.PublishedUtc = now;
        auction.UpdatedUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuctionPublishingResult.Success();
    }
}
