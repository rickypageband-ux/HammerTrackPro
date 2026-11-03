using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;

namespace HammerTrackLive.Tests;

public sealed class AuctionCatalogEditingPolicyTests
{
    [Theory]
    [InlineData(AuctionStatuses.Draft, true)]
    [InlineData(AuctionStatuses.Published, true)]
    [InlineData(AuctionStatuses.Closed, false)]
    [InlineData("Unknown", false)]
    public void LotManagement_AllowsDraftAndPublishedAuctionsOnly(string status, bool expected)
    {
        Assert.Equal(expected, AuctionCatalogEditingPolicy.CanManageLots(status));
    }
}
