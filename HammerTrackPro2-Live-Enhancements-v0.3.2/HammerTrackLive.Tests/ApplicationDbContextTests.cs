using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HammerTrackLive.Tests;

public sealed class ApplicationDbContextTests
{
    [Fact]
    public void OrganizationSlugHasUniqueIndex()
    {
        using var context = CreateContext();

        var slug = context.Model.FindEntityType(typeof(Organization))!
            .GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(Organization.Slug)]));

        Assert.True(slug.IsUnique);
    }

    [Fact]
    public void MembershipUsesOrganizationAndUserCompositeKey()
    {
        using var context = CreateContext();

        var keyNames = context.Model.FindEntityType(typeof(OrganizationMembership))!
            .FindPrimaryKey()!
            .Properties
            .Select(property => property.Name);

        Assert.Equal([nameof(OrganizationMembership.OrganizationId), nameof(OrganizationMembership.UserId)], keyNames);
    }

    [Theory]
    [InlineData(OrganizationRoles.Administrator, true)]
    [InlineData(OrganizationRoles.Staff, true)]
    [InlineData("Owner", false)]
    [InlineData("Bidder", false)]
    public void OrganizationRoleValidationAllowsOnlyPersistedRoles(string role, bool expected)
    {
        Assert.Equal(expected, OrganizationRoles.IsValid(role));
    }

    [Fact]
    public void InvitationTokenHashIsUniqueAndDoesNotExposeToken()
    {
        var first = AccountInvitationToken.Create();
        var second = AccountInvitationToken.Create();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain(first, AccountInvitationToken.Hash(first));
        Assert.Equal(AccountInvitationToken.Hash(first), AccountInvitationToken.Hash(first));
    }

    [Theory]
    [InlineData(OrganizationRoles.Administrator, 1, false)]
    [InlineData(OrganizationRoles.Administrator, 2, true)]
    [InlineData(OrganizationRoles.Staff, 1, true)]
    public void OrganizationMustRetainAnAdministrator(string role, int administratorCount, bool expected)
    {
        Assert.Equal(expected, OrganizationMembershipPolicy.CanRemoveOrDemote(role, administratorCount));
    }

    [Fact]
    public void InvitationModelHasUniqueTokenHashAndOrganizationLookupIndex()
    {
        using var context = CreateContext();
        var invitation = context.Model.FindEntityType(typeof(AccountInvitation))!;

        Assert.Contains(invitation.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(AccountInvitation.TokenHash)]));
        Assert.Contains(invitation.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(AccountInvitation.OrganizationId),
                nameof(AccountInvitation.NormalizedEmail),
                nameof(AccountInvitation.AcceptedUtc),
                nameof(AccountInvitation.ExpiresUtc)]));
    }

    [Theory]
    [InlineData("Palmetto Estate Auctions", "palmetto-estate-auctions")]
    [InlineData("  RPDAVID & Company  ", "rpdavid-company")]
    public void OrganizationSlugIsNormalized(string input, string expected)
    {
        Assert.Equal(expected, OrganizationSlug.Normalize(input));
    }

    [Fact]
    public void BootstrapPasswordMeetsConfiguredIdentityRequirements()
    {
        var password = BootstrapPassword.Create();

        Assert.True(password.Length >= 12);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, character => !char.IsLetterOrDigit(character));
    }

    [Fact]
    public void OrganizationMembershipAllowsPlatformUserToBecomeOrganizationAdministrator()
    {
        using var context = CreateContext();
        var membership = context.Model.FindEntityType(typeof(OrganizationMembership))!;

        Assert.NotNull(membership.FindPrimaryKey());
        Assert.Contains(membership.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(OrganizationMembership.OrganizationId),
                nameof(OrganizationMembership.Role)]));
    }

    [Fact]
    public void OnlineLotNumberIsUniqueWithinAuction()
    {
        using var context = CreateContext();
        var lot = context.Model.FindEntityType(typeof(OnlineLot))!;
        Assert.Contains(lot.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(OnlineLot.AuctionId), nameof(OnlineLot.LotNumber)]));
    }

    [Fact]
    public void AuctionTermsSnapshotHasOneToOneAuctionKey()
    {
        using var context = CreateContext();
        var terms = context.Model.FindEntityType(typeof(OnlineAuctionTermsSnapshot))!;
        Assert.Equal([nameof(OnlineAuctionTermsSnapshot.AuctionId)], terms.FindPrimaryKey()!.Properties.Select(property => property.Name));
    }

    [Fact]
    public void PublicationValidationRequiresCompleteAuctionAndActiveLot()
    {
        var auction = CreatePublishableAuction();
        Assert.Empty(AuctionPublicationValidator.Validate(auction));

        auction.PaymentInstructions = string.Empty;
        auction.Lots.Single().BidIncrement = 0;
        var errors = AuctionPublicationValidator.Validate(auction);
        Assert.Contains("Payment instructions are required.", errors);
        Assert.Contains(errors, error => error.Contains("invalid bidding amounts"));
    }

    [Fact]
    public void EasternAuctionTimeConvertsToUtc()
    {
        var utc = AuctionTimeZones.ToUtc(new DateTime(2026, 11, 10, 19, 0, 0), "America/New_York");
        Assert.Equal(new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero), utc);
    }

    [Fact]
    public async Task MediaStorageRejectsUnsupportedContentTypeBeforeWriting()
    {
        var storage = new LocalLotMediaStorage(Options.Create(new MediaStorageOptions { RootPath = Path.GetTempPath() }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveAsync(new MemoryStream([1, 2, 3]), "application/octet-stream", "bad.exe"));
    }

    [Fact]
    public async Task MediaStorageRejectsSpoofedImageContent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"hammertrack-media-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalLotMediaStorage(Options.Create(new MediaStorageOptions { RootPath = root }));
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveAsync(new MemoryStream("not an image"u8.ToArray()), "image/jpeg", "photo.jpg"));
            Assert.Empty(Directory.Exists(root) ? Directory.GetFiles(root) : []);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static OnlineAuction CreatePublishableAuction()
    {
        var auction = new OnlineAuction
        {
            Title = "Equipment Auction", Description = "Catalog description", Location = "Columbia, SC",
            StartsUtc = DateTimeOffset.UtcNow.AddDays(1), EndsUtc = DateTimeOffset.UtcNow.AddDays(2),
            BuyerPremiumPercent = 10, SalesTaxPercent = 8, PaymentInstructions = "Pay after checkout.",
            PickupInstructions = "Pickup by appointment."
        };
        auction.Lots.Add(new OnlineLot { LotNumber = 1, Title = "Excavator", StartingBid = 100, BidIncrement = 25, Quantity = 1 });
        return auction;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=model_test;Password=model_test")
            .Options;
        return new ApplicationDbContext(options);
    }
}
