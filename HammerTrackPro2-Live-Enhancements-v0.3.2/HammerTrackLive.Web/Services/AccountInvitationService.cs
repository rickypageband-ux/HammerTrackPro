using HammerTrackLive.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Services;

public sealed class AccountInvitationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : IAccountInvitationService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<CreatedInvitation> CreateAsync(
        Guid organizationId,
        Guid createdByUserId,
        string email,
        string displayName,
        string role,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userManager.NormalizeEmail(email.Trim());
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        if (!OrganizationRoles.IsValid(role))
        {
            throw new ArgumentException("The organization role is not valid.", nameof(role));
        }

        if (await userManager.FindByEmailAsync(email.Trim()) is not null)
        {
            throw new InvalidOperationException("That email address already belongs to a HammerTrack Online account.");
        }

        var now = timeProvider.GetUtcNow();
        var previousInvitations = await dbContext.AccountInvitations
            .Where(invitation => invitation.OrganizationId == organizationId
                && invitation.NormalizedEmail == normalizedEmail
                && invitation.AcceptedUtc == null
                && invitation.ExpiresUtc > now)
            .ToListAsync(cancellationToken);
        foreach (var previousInvitation in previousInvitations)
        {
            previousInvitation.ExpiresUtc = now;
        }

        var token = AccountInvitationToken.Create();
        var invitation = new AccountInvitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CreatedByUserId = createdByUserId,
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName.Trim(),
            Role = role,
            TokenHash = AccountInvitationToken.Hash(token),
            CreatedUtc = now,
            ExpiresUtc = now.Add(InvitationLifetime)
        };
        dbContext.AccountInvitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreatedInvitation(invitation, token);
    }

    public async Task<InvitationDetails?> FindValidAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = AccountInvitationToken.Hash(token);
        var now = timeProvider.GetUtcNow();
        return await dbContext.AccountInvitations
            .AsNoTracking()
            .Where(invitation => invitation.TokenHash == tokenHash
                && invitation.AcceptedUtc == null
                && invitation.ExpiresUtc > now
                && invitation.Organization.IsActive)
            .Select(invitation => new InvitationDetails(
                invitation.Id,
                invitation.Email,
                invitation.DisplayName,
                invitation.Organization.Name,
                invitation.Role,
                invitation.ExpiresUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<InvitationAcceptanceResult> AcceptAsync(
        string token,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvitationAcceptanceResult.Failure("This invitation is invalid or has expired.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tokenHash = AccountInvitationToken.Hash(token);
        var now = timeProvider.GetUtcNow();
        var invitation = await dbContext.AccountInvitations
            .Include(item => item.Organization)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
        if (invitation is null
            || invitation.AcceptedUtc is not null
            || invitation.ExpiresUtc <= now
            || !invitation.Organization.IsActive)
        {
            return InvitationAcceptanceResult.Failure("This invitation is invalid or has expired.");
        }

        if (await userManager.FindByEmailAsync(invitation.Email) is not null)
        {
            return InvitationAcceptanceResult.Failure("An account already exists for this email address. Sign in or reset its password.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = invitation.Email,
            Email = invitation.Email,
            EmailConfirmed = true,
            DisplayName = invitation.DisplayName,
            CreatedUtc = now,
            IsActive = true
        };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return InvitationAcceptanceResult.Failure(string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        dbContext.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrganizationId = invitation.OrganizationId,
            UserId = user.Id,
            Role = invitation.Role,
            CreatedUtc = now
        });
        invitation.AcceptedUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return InvitationAcceptanceResult.Success(user);
    }

}
