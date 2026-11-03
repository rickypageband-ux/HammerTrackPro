namespace HammerTrackLive.Web.Data;

public sealed class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<OrganizationMembership> Memberships { get; } = [];

    public ICollection<AccountInvitation> Invitations { get; } = [];

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PublicEmail { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<OnlineAuction> Auctions { get; } = [];
}
