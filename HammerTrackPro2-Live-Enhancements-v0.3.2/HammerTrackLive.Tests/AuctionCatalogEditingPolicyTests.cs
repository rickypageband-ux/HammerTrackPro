using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;

namespace HammerTrackLive.Tests;

public sealed class AuctionCatalogEditingPolicyTests
{
    [Fact]
    public void LotEditorProvidesCommonCategoryAndConditionChoices()
    {
        Assert.Contains("Toys & Games", LotCatalogChoices.Categories);
        Assert.Contains("Electronics", LotCatalogChoices.Categories);
        Assert.Contains("Clothing & Accessories", LotCatalogChoices.Categories);
        Assert.Contains("Household", LotCatalogChoices.Categories);
        Assert.Contains("New", LotCatalogChoices.Conditions);
        Assert.Contains("Used", LotCatalogChoices.Conditions);
        Assert.True(LotCatalogChoices.MaximumFilesPerUpload > 1);
        Assert.True(LotCatalogChoices.MaximumFilesPerLot >= LotCatalogChoices.MaximumFilesPerUpload);
    }

    [Theory]
    [InlineData(new int[] { }, 1)]
    [InlineData(new[] { 2 }, 3)]
    [InlineData(new[] { 1, 2, 3, 4 }, 5)]
    [InlineData(new[] { 1, 2, 4 }, 5)]
    public void NewLotContinuesAfterHighestExistingNumber(int[] existing, int expected)
    {
        Assert.Equal(expected, LotCatalogChoices.GetNextLotNumber(existing));
    }

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
