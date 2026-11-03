using System.ComponentModel.DataAnnotations;
using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Pages.Platform;

[Authorize(Policy = "PlatformAdministrator")]
public sealed class OrganizationsModel(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAccountInvitationService invitationService,
    IAccountEmailSender emailSender,
    ILogger<OrganizationsModel> logger) : PageModel
{
    [BindProperty]
    public CreateOrganizationInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<OrganizationView> Organizations { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        string slug;
        try
        {
            slug = OrganizationSlug.Normalize(Input.Slug);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(Input.Slug), exception.Message);
            await LoadAsync();
            return Page();
        }

        if (await dbContext.Organizations.AnyAsync(item => item.Slug == slug, HttpContext.RequestAborted))
        {
            ModelState.AddModelError(nameof(Input.Slug), "That organization address is already in use.");
            await LoadAsync();
            return Page();
        }

        var currentUser = await userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The signed-in account could not be loaded.");
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = Input.Name.Trim(),
            Slug = slug,
            CreatedUtc = DateTimeOffset.UtcNow,
            IsActive = true
        };
        CreatedInvitation? invitation = null;
        var existingAdministrator = await userManager.FindByEmailAsync(Input.AdministratorEmail.Trim());
        await using (var transaction = await dbContext.Database.BeginTransactionAsync(HttpContext.RequestAborted))
        {
            try
            {
                dbContext.Organizations.Add(organization);
                await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
                if (existingAdministrator is not null)
                {
                    if (!existingAdministrator.IsActive || !existingAdministrator.EmailConfirmed)
                    {
                        throw new InvalidOperationException("The existing administrator account is not active and confirmed.");
                    }

                    dbContext.OrganizationMemberships.Add(new OrganizationMembership
                    {
                        OrganizationId = organization.Id,
                        UserId = existingAdministrator.Id,
                        Role = OrganizationRoles.Administrator,
                        CreatedUtc = DateTimeOffset.UtcNow
                    });
                    await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
                }
                else
                {
                    invitation = await invitationService.CreateAsync(
                        organization.Id,
                        currentUser.Id,
                        Input.AdministratorEmail,
                        Input.AdministratorName,
                        OrganizationRoles.Administrator,
                        HttpContext.RequestAborted);
                }
                await transaction.CommitAsync(HttpContext.RequestAborted);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or DbUpdateException)
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);
                logger.LogWarning(exception, "Auction company provisioning failed for slug {Slug}.", slug);
                ModelState.AddModelError(string.Empty, "The auction company could not be created. Verify the administrator email and organization address.");
                await LoadAsync();
                return Page();
            }
        }

        if (invitation is null)
        {
            StatusMessage = $"{organization.Name} was created and the existing administrator account was assigned.";
            return RedirectToPage();
        }

        var invitationUrl = Url.Page(
            "/Account/AcceptInvitation",
            pageHandler: null,
            values: new { token = invitation.Token },
            protocol: Request.Scheme)
            ?? throw new InvalidOperationException("The invitation URL could not be created.");
        try
        {
            await emailSender.SendInvitationAsync(
                invitation.Invitation.Email,
                invitation.Invitation.DisplayName,
                organization.Name,
                invitationUrl);
            StatusMessage = $"{organization.Name} was created and its administrator invitation was sent.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Administrator invitation email failed for organization {OrganizationId}.", organization.Id);
            StatusMessage = $"{organization.Name} was created, but its invitation email was not delivered. Open Manage and send a new invitation after checking email settings.";
        }
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Organizations = await dbContext.Organizations
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new OrganizationView(item.Id, item.Name, item.Memberships.Count))
            .ToListAsync(HttpContext.RequestAborted);
    }

    public sealed class CreateOrganizationInput
    {
        [Required, StringLength(160)]
        [Display(Name = "Auction company name")]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Organization address")]
        public string Slug { get; set; } = string.Empty;

        [Required, StringLength(120)]
        [Display(Name = "Administrator name")]
        public string AdministratorName { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Administrator email")]
        public string AdministratorEmail { get; set; } = string.Empty;
    }

    public sealed record OrganizationView(Guid Id, string Name, int MemberCount);
}
