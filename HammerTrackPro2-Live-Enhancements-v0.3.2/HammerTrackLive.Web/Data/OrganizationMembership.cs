namespace HammerTrackLive.Web.Data;

public sealed class OrganizationMembership
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string Role { get; set; } = OrganizationRoles.Staff;

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
