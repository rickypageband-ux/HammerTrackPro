using System.ComponentModel.DataAnnotations;
using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Pages.Auctioneer;

[Authorize]
public sealed class ProfileModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IOrganizationAdministrationService authorization) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid OrganizationId { get; set; }
    [BindProperty] public ProfileInput Input { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CanAdministerAsync()) return Forbid();
        var organization = await dbContext.Organizations.AsNoTracking().SingleOrDefaultAsync(item => item.Id == OrganizationId, HttpContext.RequestAborted);
        if (organization is null) return NotFound();
        Input = new ProfileInput
        {
            Name = organization.Name, Address = organization.Address, City = organization.City, State = organization.State,
            PostalCode = organization.PostalCode, Phone = organization.Phone, PublicEmail = organization.PublicEmail,
            Website = organization.Website, Description = organization.Description
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await CanAdministerAsync()) return Forbid();
        if (!OrganizationProfileInputNormalizer.TryNormalizeWebsite(Input.Website, out var normalizedWebsite))
        {
            ModelState.AddModelError("Input.Website", "Enter a valid website address, such as hammertrackonline.com.");
        }
        if (!ModelState.IsValid) return Page();
        var organization = await dbContext.Organizations.SingleOrDefaultAsync(item => item.Id == OrganizationId, HttpContext.RequestAborted);
        if (organization is null) return NotFound();
        organization.Name = Input.Name.Trim(); organization.Address = Input.Address?.Trim() ?? string.Empty; organization.City = Input.City?.Trim() ?? string.Empty;
        organization.State = Input.State?.Trim() ?? string.Empty; organization.PostalCode = Input.PostalCode?.Trim() ?? string.Empty; organization.Phone = Input.Phone?.Trim() ?? string.Empty;
        organization.PublicEmail = Input.PublicEmail?.Trim() ?? string.Empty; organization.Website = normalizedWebsite; organization.Description = Input.Description?.Trim() ?? string.Empty;
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
        StatusMessage = "Company profile saved.";
        return RedirectToPage(new { organizationId = OrganizationId });
    }

    private async Task<bool> CanAdministerAsync()
    {
        var user = await userManager.GetUserAsync(User);
        return user is not null && await authorization.CanAdministerAsync(user.Id, OrganizationId, await userManager.IsInRoleAsync(user, PlatformRoles.Administrator), HttpContext.RequestAborted);
    }

    public sealed class ProfileInput
    {
        [Required, StringLength(160)] public string Name { get; set; } = string.Empty;
        [StringLength(200)] public string? Address { get; set; }
        [StringLength(100)] public string? City { get; set; }
        [StringLength(50)] public string? State { get; set; }
        [StringLength(20), Display(Name = "ZIP / postal code")] public string? PostalCode { get; set; }
        [Phone, StringLength(30)] public string? Phone { get; set; }
        [EmailAddress, StringLength(256), Display(Name = "Public email")] public string? PublicEmail { get; set; }
        [StringLength(300)] public string? Website { get; set; }
        [StringLength(2000)] public string? Description { get; set; }
    }
}
