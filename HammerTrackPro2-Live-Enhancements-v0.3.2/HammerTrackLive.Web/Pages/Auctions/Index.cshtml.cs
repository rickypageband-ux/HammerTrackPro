using HammerTrackLive.Web.Models;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HammerTrackLive.Web.Pages.Auctions;

public sealed class IndexModel(IAuctionCatalogService catalog) : PageModel
{
    public IReadOnlyList<AuctionSummary> Auctions { get; private set; } = [];

    public async Task OnGetAsync() => Auctions = await catalog.GetFeaturedAuctionsAsync(HttpContext.RequestAborted);
}
