using Microsoft.AspNetCore.Identity;

namespace HammerTrackLive.Web.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<OrganizationMembership> OrganizationMemberships { get; } = [];
}
