namespace HammerTrackLive.Web.Data;

public sealed class OnlineLotMedia
{
    public Guid Id { get; set; }
    public Guid LotId { get; set; }
    public OnlineLot Lot { get; set; } = null!;
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Kind { get; set; } = "Image";
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
