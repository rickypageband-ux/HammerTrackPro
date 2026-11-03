namespace HammerTrackLive.Web.Data;

public sealed class AccountInvitation
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = OrganizationRoles.Staff;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; set; }

    public DateTimeOffset? AcceptedUtc { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid CreatedByUserId { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;
}
