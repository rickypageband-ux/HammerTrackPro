using HammerTrackLive.Web.Data;

namespace HammerTrackLive.Web.Services;

public static class AuctionCatalogEditingPolicy
{
    public static bool CanManageLots(string status) =>
        status is AuctionStatuses.Draft or AuctionStatuses.Published;
}
