using HammerTrackLive.Web.Data;

namespace HammerTrackLive.Web.Services;

public interface IAccountInvitationService
{
    Task<CreatedInvitation> CreateAsync(
        Guid organizationId,
        Guid createdByUserId,
        string email,
        string displayName,
        string role,
        CancellationToken cancellationToken = default);

    Task<InvitationDetails?> FindValidAsync(string token, CancellationToken cancellationToken = default);

    Task<InvitationAcceptanceResult> AcceptAsync(
        string token,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record CreatedInvitation(AccountInvitation Invitation, string Token);

public sealed record InvitationDetails(
    Guid InvitationId,
    string Email,
    string DisplayName,
    string OrganizationName,
    string Role,
    DateTimeOffset ExpiresUtc);

public sealed record InvitationAcceptanceResult(bool Succeeded, string? Error, ApplicationUser? User)
{
    public static InvitationAcceptanceResult Success(ApplicationUser user) => new(true, null, user);

    public static InvitationAcceptanceResult Failure(string error) => new(false, error, null);
}
