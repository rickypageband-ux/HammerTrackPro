using HammerTrackLive.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Services;

public sealed class OrganizationAdministrationService(ApplicationDbContext dbContext)
    : IOrganizationAdministrationService
{
    public Task<bool> CanAdministerAsync(
        Guid userId,
        Guid organizationId,
        bool isPlatformAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (isPlatformAdministrator)
        {
            return Task.FromResult(true);
        }

        return dbContext.OrganizationMemberships.AnyAsync(
            membership => membership.OrganizationId == organizationId
                && membership.UserId == userId
                && membership.Role == OrganizationRoles.Administrator
                && membership.Organization.IsActive,
            cancellationToken);
    }

    public async Task<OrganizationAdministrationResult> ChangeRoleAsync(
        Guid organizationId,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationRoles.IsValid(role))
        {
            return OrganizationAdministrationResult.Failure("Select Administrator or Staff.");
        }

        var membership = await dbContext.OrganizationMemberships.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.UserId == userId,
            cancellationToken);
        if (membership is null)
        {
            return OrganizationAdministrationResult.Failure("That organization member no longer exists.");
        }

        if (role != OrganizationRoles.Administrator
            && !OrganizationMembershipPolicy.CanRemoveOrDemote(
                membership.Role,
                await CountAdministratorsAsync(organizationId, cancellationToken)))
        {
            return OrganizationAdministrationResult.Failure("Every organization must retain at least one administrator.");
        }

        membership.Role = role;
        await dbContext.SaveChangesAsync(cancellationToken);
        return OrganizationAdministrationResult.Success("The member role was updated.");
    }

    public async Task<OrganizationAdministrationResult> RemoveMemberAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.OrganizationMemberships.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.UserId == userId,
            cancellationToken);
        if (membership is null)
        {
            return OrganizationAdministrationResult.Failure("That organization member no longer exists.");
        }

        if (!OrganizationMembershipPolicy.CanRemoveOrDemote(
            membership.Role,
            await CountAdministratorsAsync(organizationId, cancellationToken)))
        {
            return OrganizationAdministrationResult.Failure("The final organization administrator cannot be removed.");
        }

        dbContext.OrganizationMemberships.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OrganizationAdministrationResult.Success("The member was removed from this organization.");
    }

    private Task<int> CountAdministratorsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return dbContext.OrganizationMemberships.CountAsync(
            item => item.OrganizationId == organizationId && item.Role == OrganizationRoles.Administrator,
            cancellationToken);
    }
}
