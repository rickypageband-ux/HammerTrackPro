namespace HammerTrackLive.Web.Services;

public interface IOrganizationAdministrationService
{
    Task<bool> CanAdministerAsync(Guid userId, Guid organizationId, bool isPlatformAdministrator, CancellationToken cancellationToken = default);

    Task<OrganizationAdministrationResult> ChangeRoleAsync(Guid organizationId, Guid userId, string role, CancellationToken cancellationToken = default);

    Task<OrganizationAdministrationResult> RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed record OrganizationAdministrationResult(bool Succeeded, string Message)
{
    public static OrganizationAdministrationResult Success(string message) => new(true, message);

    public static OrganizationAdministrationResult Failure(string message) => new(false, message);
}
