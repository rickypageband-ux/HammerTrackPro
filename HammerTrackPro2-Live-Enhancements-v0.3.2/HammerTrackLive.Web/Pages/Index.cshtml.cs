using HammerTrackLive.Web.Models;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HammerTrackLive.Web.Pages;

public sealed class IndexModel(IAuctionCatalogService catalog) : PageModel
{
    public IReadOnlyList<AuctionSummary> Auctions { get; private set; } = [];

    public void OnGet()
    {
        Auctions = catalog.GetFeaturedAuctions();
    }
}
