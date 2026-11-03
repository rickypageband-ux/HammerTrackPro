using HammerTrackLive.Web.Models;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HammerTrackLive.Web.Pages.Auctions;

public sealed class DetailsModel(IAuctionCatalogService catalog) : PageModel
{
    public AuctionSummary Auction { get; private set; } = null!;

    public IActionResult OnGet(string slug)
    {
        var auction = catalog.GetBySlug(slug);
        if (auction is null)
        {
            return NotFound();
        }

        Auction = auction;
        return Page();
    }
}
