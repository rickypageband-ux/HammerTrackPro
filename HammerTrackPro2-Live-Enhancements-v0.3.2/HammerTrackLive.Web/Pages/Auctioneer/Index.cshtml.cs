using HammerTrackLive.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Pages.Auctioneer;

[Authorize]
public sealed class IndexModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) : PageModel
{
    public IReadOnlyList<OrganizationView> Organizations { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The signed-in account could not be loaded.");
        var platformAdministrator = await userManager.IsInRoleAsync(user, PlatformRoles.Administrator);
        Organizations = await dbContext.Organizations
            .AsNoTracking()
            .Where(organization => organization.IsActive
                && (platformAdministrator || organization.Memberships.Any(membership => membership.UserId == user.Id)))
            .OrderBy(organization => organization.Name)
            .Select(organization => new OrganizationView(
                organization.Id,
                organization.Name,
                organization.Auctions.OrderByDescending(auction => auction.UpdatedUtc)
                    .Select(auction => new AuctionView(auction.Id, auction.Title, auction.Status, auction.Lots.Count(lot => lot.IsActive)))
                    .ToList()))
            .ToListAsync(HttpContext.RequestAborted);
    }

    public sealed record OrganizationView(Guid Id, string Name, IReadOnlyList<AuctionView> Auctions);
    public sealed record AuctionView(Guid Id, string Title, string Status, int LotCount);
}
