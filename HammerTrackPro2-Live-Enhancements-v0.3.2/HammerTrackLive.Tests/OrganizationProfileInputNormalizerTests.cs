using HammerTrackLive.Web.Services;
using HammerTrackLive.Web.Pages.Auctioneer;
using System.Reflection;

namespace HammerTrackLive.Tests;

public sealed class OrganizationProfileInputNormalizerTests
{
    [Fact]
    public void ProfileFieldsOtherThanName_AreNullableAndOptionalForModelBinding()
    {
        var nullability = new NullabilityInfoContext();
        var optionalProperties = typeof(ProfileModel.ProfileInput).GetProperties()
            .Where(property => property.Name != nameof(ProfileModel.ProfileInput.Name));

        Assert.All(optionalProperties, property =>
            Assert.Equal(NullabilityState.Nullable, nullability.Create(property).WriteState));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("hammertrackonline.com", "https://hammertrackonline.com")]
    [InlineData("https://hammertrackonline.com", "https://hammertrackonline.com")]
    [InlineData(" http://example.com/contact ", "http://example.com/contact")]
    public void WebsiteAddress_IsNormalizedForCommonAuctioneerInput(string input, string expected)
    {
        var valid = OrganizationProfileInputNormalizer.TryNormalizeWebsite(input, out var normalized);

        Assert.True(valid);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("not a website")]
    [InlineData("ftp://example.com")]
    public void InvalidOrUnsupportedWebsiteAddress_IsRejected(string input)
    {
        Assert.False(OrganizationProfileInputNormalizer.TryNormalizeWebsite(input, out _));
    }
}
