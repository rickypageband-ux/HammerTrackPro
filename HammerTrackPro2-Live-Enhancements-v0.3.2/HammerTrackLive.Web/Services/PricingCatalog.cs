using HammerTrackLive.Web.Models;

namespace HammerTrackLive.Web.Services;

public static class PricingCatalog
{
    public static IReadOnlyList<ServicePlan> GetPlans() =>
    [
        new(
            "HammerTrack Online",
            "$100/month",
            "Timed online auctions with an auction-company storefront and integrated checkout.",
            [
                "2% of online hammer sales",
                "No per-bid platform fee at launch",
                "25 GB catalog and image storage included",
                "Card processing billed separately by the selected provider"
            ],
            true),
        new(
            "HammerTrack Live",
            "$100/month + $75/event",
            "Camera-enabled live auctions for remote and in-room bidders.",
            [
                "Remote bidder camera and paddle queue",
                "Auctioneer-controlled bid acceptance",
                "No percentage of live-event sales",
                "Seller provides payment instructions after the event"
            ])
    ];
}
