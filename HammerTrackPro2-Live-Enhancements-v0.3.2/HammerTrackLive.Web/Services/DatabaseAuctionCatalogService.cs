using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Services;

public sealed class DatabaseAuctionCatalogService(ApplicationDbContext dbContext) : IAuctionCatalogService
{
    public async Task<IReadOnlyList<AuctionSummary>> GetFeaturedAuctionsAsync(CancellationToken cancellationToken = default)
    {
        return await Query().ToListAsync(cancellationToken);
    }

    public async Task<AuctionDetails?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var auction = await dbContext.OnlineAuctions.AsNoTracking()
            .Include(item => item.Organization)
            .Include(item => item.TermsSnapshot)
            .Include(item => item.Lots.Where(lot => lot.IsActive)).ThenInclude(lot => lot.Media)
            .SingleOrDefaultAsync(item => item.Slug == slug && item.Status == AuctionStatuses.Published && item.Organization.IsActive, cancellationToken);
        if (auction?.TermsSnapshot is null) return null;
        var firstImage = auction.Lots.SelectMany(lot => lot.Media).Where(media => media.Kind == "Image").OrderBy(media => media.SortOrder).FirstOrDefault();
        var summary = new AuctionSummary(auction.Slug, auction.Title, auction.Organization.Name, auction.Location, auction.StartsUtc, auction.EndsUtc,
            auction.Lots.Count(lot => lot.IsActive), "Online Auction", auction.EndsUtc <= DateTimeOffset.UtcNow ? "Closed" : auction.StartsUtc <= DateTimeOffset.UtcNow ? "Open" : "Coming Soon",
            auction.ShippingAvailable, firstImage is null ? "/images/auction-tools.svg" : $"/Media/Download?id={firstImage.Id}", auction.Description);
        var lots = auction.Lots.Where(lot => lot.IsActive).OrderBy(lot => lot.LotNumber)
            .Select(lot => new PublicLot(lot.LotNumber, lot.Title, lot.Description, lot.Category, lot.Condition, lot.StartingBid, lot.BidIncrement, lot.Quantity,
                lot.Media.OrderBy(media => media.SortOrder).Select(media => new PublicMedia(media.Id, media.OriginalFileName, media.Kind)).ToList())).ToList();
        var terms = auction.TermsSnapshot;
        return new AuctionDetails(summary, terms.BuyerPremiumPercent, terms.SalesTaxPercent, terms.TaxBasis, terms.PaymentInstructions, terms.PickupInstructions, terms.ShippingInstructions, lots);
    }

    private IQueryable<AuctionSummary> Query()
    {
        return dbContext.OnlineAuctions
            .AsNoTracking()
            .Where(auction => auction.Status == AuctionStatuses.Published && auction.Organization.IsActive)
            .OrderBy(auction => auction.EndsUtc)
            .Select(auction => new AuctionSummary(
                auction.Slug,
                auction.Title,
                auction.Organization.Name,
                auction.Location,
                auction.StartsUtc,
                auction.EndsUtc,
                auction.Lots.Count(lot => lot.IsActive),
                "Online Auction",
                auction.EndsUtc <= DateTimeOffset.UtcNow ? "Closed" : auction.StartsUtc <= DateTimeOffset.UtcNow ? "Open" : "Coming Soon",
                auction.ShippingAvailable,
                auction.Lots.Where(lot => lot.IsActive)
                    .SelectMany(lot => lot.Media)
                    .Where(media => media.Kind == "Image")
                    .OrderBy(media => media.SortOrder)
                    .Select(media => "/Media/Download?id=" + media.Id)
                    .FirstOrDefault() ?? "/images/auction-tools.svg",
                auction.Description));
    }
}
