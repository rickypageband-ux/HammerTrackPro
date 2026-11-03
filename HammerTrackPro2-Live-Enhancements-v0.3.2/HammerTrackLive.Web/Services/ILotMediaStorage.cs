namespace HammerTrackLive.Web.Services;

public interface ILotMediaStorage
{
    Task<StoredMedia> SaveAsync(Stream source, string contentType, string originalFileName, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record StoredMedia(string StorageKey, string OriginalFileName, string ContentType, long SizeBytes, string Kind);
