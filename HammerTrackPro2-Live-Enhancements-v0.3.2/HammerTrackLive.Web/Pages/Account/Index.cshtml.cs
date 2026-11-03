using HammerTrackLive.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Pages.Account;

[Authorize]
public sealed class IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) : PageModel
{
    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public IReadOnlyList<MembershipView> Memberships { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The signed-in account could not be loaded.");
        DisplayName = user.DisplayName;
        Email = user.Email ?? string.Empty;
        Memberships = await dbContext.OrganizationMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == user.Id && membership.Organization.IsActive)
            .OrderBy(membership => membership.Organization.Name)
            .Select(membership => new MembershipView(membership.Organization.Name, membership.Role))
            .ToListAsync();
    }

    public sealed record MembershipView(string OrganizationName, string Role);
}
