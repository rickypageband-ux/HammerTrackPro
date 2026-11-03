using HammerTrackLive.Web.Models;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HammerTrackLive.Web.Pages;

public sealed class PricingModel : PageModel
{
    public IReadOnlyList<ServicePlan> Plans { get; } = PricingCatalog.GetPlans();
}
