using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Pages.Media;

public sealed class DownloadModel(ApplicationDbContext dbContext, ILotMediaStorage storage, UserManager<ApplicationUser> userManager) : PageModel
{
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var media = await dbContext.OnlineLotMedia.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.StorageKey, item.OriginalFileName, item.ContentType, item.Lot.Auction.Status, item.Lot.Auction.OrganizationId })
            .SingleOrDefaultAsync(HttpContext.RequestAborted);
        if (media is null) return NotFound();
        if (media.Status != AuctionStatuses.Published)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null || (!await userManager.IsInRoleAsync(user, PlatformRoles.Administrator)
                && !await dbContext.OrganizationMemberships.AnyAsync(item => item.OrganizationId == media.OrganizationId && item.UserId == user.Id && item.Role == OrganizationRoles.Administrator, HttpContext.RequestAborted))) return NotFound();
        }
        var stream = await storage.OpenReadAsync(media.StorageKey, HttpContext.RequestAborted);
        if (stream is null) return NotFound();
        return new FileStreamResult(stream, media.ContentType)
        {
            FileDownloadName = media.ContentType == "application/pdf" ? media.OriginalFileName : null,
            EnableRangeProcessing = true
        };
    }
}
