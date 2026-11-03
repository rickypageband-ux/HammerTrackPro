namespace HammerTrackLive.Web.Services;

public interface IAuctionPublishingService
{
    Task<AuctionPublishingResult> PublishAsync(Guid organizationId, Guid auctionId, CancellationToken cancellationToken = default);
}

public sealed record AuctionPublishingResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static AuctionPublishingResult Success() => new(true, []);
    public static AuctionPublishingResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
