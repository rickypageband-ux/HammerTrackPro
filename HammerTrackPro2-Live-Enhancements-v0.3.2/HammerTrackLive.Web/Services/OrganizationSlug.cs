using System.Text.RegularExpressions;

namespace HammerTrackLive.Web.Services;

public static partial class OrganizationSlug
{
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var slug = InvalidCharacterRegex().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
        if (slug.Length is < 3 or > 100)
        {
            throw new ArgumentException("The organization address must contain 3 to 100 letters, numbers, or hyphens.", nameof(value));
        }

        return slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidCharacterRegex();
}
