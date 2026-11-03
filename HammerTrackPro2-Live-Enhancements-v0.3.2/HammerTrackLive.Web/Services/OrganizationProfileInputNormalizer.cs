namespace HammerTrackLive.Web.Services;

public static class OrganizationProfileInputNormalizer
{
    public static bool TryNormalizeWebsite(string? value, out string normalized)
    {
        normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return true;

        if (!normalized.Contains("://", StringComparison.Ordinal))
        {
            normalized = $"https://{normalized}";
        }

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
