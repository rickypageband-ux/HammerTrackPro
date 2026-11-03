using HammerTrackLive.Web.Data;
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=model_test;Password=model_test")
            .Options;
        return new ApplicationDbContext(options);
    }
}
