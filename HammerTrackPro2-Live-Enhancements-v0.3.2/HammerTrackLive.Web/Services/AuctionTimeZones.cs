namespace HammerTrackLive.Web.Services;

public static class AuctionTimeZones
{
    public static readonly IReadOnlyList<(string Id, string Name)> Supported =
    [
        ("America/New_York", "Eastern"),
        ("America/Chicago", "Central"),
        ("America/Denver", "Mountain"),
        ("America/Los_Angeles", "Pacific")
    ];

    public static DateTimeOffset ToUtc(DateTime localDateTime, string timeZoneId)
    {
        if (!Supported.Any(item => item.Id == timeZoneId)) throw new ArgumentException("Select a supported time zone.", nameof(timeZoneId));
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        if (zone.IsInvalidTime(unspecified)) throw new ArgumentException("The selected time does not exist because of daylight saving time.");
        return new DateTimeOffset(unspecified, zone.GetUtcOffset(unspecified)).ToUniversalTime();
    }

    public static DateTime ToLocal(DateTimeOffset utc, string timeZoneId)
    {
        return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).DateTime;
    }
}
