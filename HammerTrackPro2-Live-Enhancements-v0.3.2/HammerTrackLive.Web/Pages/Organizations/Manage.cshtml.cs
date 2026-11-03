using System.ComponentModel.DataAnnotations;
using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Pages.Organizations;

[Authorize]
public sealed class ManageModel(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOrganizationAdministrationService organizationAdministration,
    IAccountInvitationService invitationService,
    IAccountEmailSender emailSender,
    ILogger<ManageModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid OrganizationId { get; set; }

    [BindProperty]
    public InvitationInput Invite { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string OrganizationName { get; private set; } = string.Empty;

    public IReadOnlyList<MemberView> Members { get; private set; } = [];

    public IReadOnlyList<PendingInvitationView> PendingInvitations { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadOrForbidAsync() ? Page() : Forbid();
    }

    public async Task<IActionResult> OnPostInviteAsync()
    {
        if (!await CanAdministerAsync())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var currentUser = await userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The signed-in account could not be loaded.");
        try
        {
            var created = await invitationService.CreateAsync(
                OrganizationId,
                currentUser.Id,
                Invite.Email,
                Invite.DisplayName,
                Invite.Role,
                HttpContext.RequestAborted);
            var invitationUrl = Url.Page(
                "/Account/AcceptInvitation",
                pageHandler: null,
                values: new { token = created.Token },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("The invitation URL could not be created.");
            await emailSender.SendInvitationAsync(
                created.Invitation.Email,
                created.Invitation.DisplayName,
                await GetOrganizationNameAsync(),
                invitationUrl);
            StatusMessage = $"A private invitation was sent to {created.Invitation.Email}.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            logger.LogWarning(exception, "Organization invitation could not be completed for organization {OrganizationId}.", OrganizationId);
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync();
            return Page();
        }

        return RedirectToPage(new { organizationId = OrganizationId });
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(Guid userId, string role)
    {
        if (!await CanAdministerAsync())
        {
            return Forbid();
        }

        var result = await organizationAdministration.ChangeRoleAsync(
            OrganizationId,
            userId,
            role,
            HttpContext.RequestAborted);
        StatusMessage = result.Message;
        return RedirectToPage(new { organizationId = OrganizationId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid userId)
    {
        if (!await CanAdministerAsync())
        {
            return Forbid();
        }

        var result = await organizationAdministration.RemoveMemberAsync(
            OrganizationId,
            userId,
            HttpContext.RequestAborted);
        StatusMessage = result.Message;
        return RedirectToPage(new { organizationId = OrganizationId });
    }

    private async Task<bool> LoadOrForbidAsync()
    {
        if (!await CanAdministerAsync())
        {
            return false;
        }

        await LoadAsync();
        return true;
    }

    private async Task<bool> CanAdministerAsync()
    {
        var user = await userManager.GetUserAsync(User);
        return user is not null && await organizationAdministration.CanAdministerAsync(
            user.Id,
            OrganizationId,
            User.IsInRole(PlatformRoles.Administrator),
            HttpContext.RequestAborted);
    }

    private async Task LoadAsync()
    {
        OrganizationName = await GetOrganizationNameAsync();
        Members = await dbContext.OrganizationMemberships
            .AsNoTracking()
            .Where(item => item.OrganizationId == OrganizationId)
            .OrderBy(item => item.User.DisplayName)
            .Select(item => new MemberView(item.UserId, item.User.DisplayName, item.User.Email ?? string.Empty, item.Role))
            .ToListAsync(HttpContext.RequestAborted);
        var now = DateTimeOffset.UtcNow;
        PendingInvitations = await dbContext.AccountInvitations
            .AsNoTracking()
            .Where(item => item.OrganizationId == OrganizationId && item.AcceptedUtc == null && item.ExpiresUtc > now)
            .OrderBy(item => item.Email)
            .Select(item => new PendingInvitationView(item.Email, item.Role, item.ExpiresUtc))
            .ToListAsync(HttpContext.RequestAborted);
    }

    private async Task<string> GetOrganizationNameAsync()
    {
        return await dbContext.Organizations
            .Where(item => item.Id == OrganizationId && item.IsActive)
            .Select(item => item.Name)
            .SingleOrDefaultAsync(HttpContext.RequestAborted)
            ?? throw new InvalidOperationException("The auction company could not be found.");
    }

    public sealed class InvitationInput
    {
        [Required, StringLength(120)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = OrganizationRoles.Staff;
    }

    public sealed record MemberView(Guid UserId, string DisplayName, string Email, string Role);

    public sealed record PendingInvitationView(string Email, string Role, DateTimeOffset ExpiresUtc);
}
