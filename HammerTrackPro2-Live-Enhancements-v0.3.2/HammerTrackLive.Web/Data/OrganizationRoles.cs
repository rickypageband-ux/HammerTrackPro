namespace HammerTrackLive.Web.Data;

public static class OrganizationRoles
{
    public const string Administrator = "Administrator";
    public const string Staff = "Staff";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Administrator,
        Staff
    };

    public static bool IsValid(string role) => All.Contains(role);
}
