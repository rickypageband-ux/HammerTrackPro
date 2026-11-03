using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.EntityFrameworkCore;

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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=model_test;Password=model_test")
            .Options;
        return new ApplicationDbContext(options);
    }
}
