namespace HammerTrackLive.Web.Services;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";
    public string RootPath { get; set; } = string.Empty;
    public long MaximumFileBytes { get; set; } = 15 * 1024 * 1024;
}
