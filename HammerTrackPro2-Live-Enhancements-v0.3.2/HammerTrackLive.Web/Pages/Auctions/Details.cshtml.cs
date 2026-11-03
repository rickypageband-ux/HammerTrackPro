using HammerTrackLive.Web.Models;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HammerTrackLive.Web.Pages.Auctions;

public sealed class DetailsModel(IAuctionCatalogService catalog) : PageModel
{
    public AuctionDetails Details { get; private set; } = null!;
    public AuctionSummary Auction => Details.Summary;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var auction = await catalog.GetBySlugAsync(slug, HttpContext.RequestAborted);
        if (auction is null)
        {
            return NotFound();
        }

        Details = auction;
        return Page();
    }
}
