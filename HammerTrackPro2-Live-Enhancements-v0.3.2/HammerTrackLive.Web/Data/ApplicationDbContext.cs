using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("b8bf5ce4-4fd2-4f0f-81c7-bd59f4991da7"),
                Name = PlatformRoles.Administrator,
                NormalizedName = PlatformRoles.Administrator.ToUpperInvariant(),
                ConcurrencyStamp = "platform-administrator-v1"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("a1643637-55c4-4f80-af5c-60da2bd7dd2d"),
                Name = PlatformRoles.Bidder,
                NormalizedName = PlatformRoles.Bidder.ToUpperInvariant(),
                ConcurrencyStamp = "bidder-v1"
            });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.CreatedUtc).IsRequired();
            entity.Property(user => user.IsActive).HasDefaultValue(true).IsRequired();
        });

        builder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(organization => organization.Id);
            entity.Property(organization => organization.Name).HasMaxLength(160).IsRequired();
            entity.Property(organization => organization.Slug).HasMaxLength(100).IsRequired();
            entity.Property(organization => organization.CreatedUtc).IsRequired();
            entity.Property(organization => organization.IsActive).HasDefaultValue(true).IsRequired();
            entity.HasIndex(organization => organization.Slug).IsUnique();
            entity.HasIndex(organization => new { organization.IsActive, organization.Name });
        });

        builder.Entity<OrganizationMembership>(entity =>
        {
            entity.ToTable("OrganizationMemberships", table =>
                table.HasCheckConstraint(
                    "CK_OrganizationMemberships_Role",
                    "\"Role\" IN ('Administrator', 'Staff')"));
            entity.HasKey(membership => new { membership.OrganizationId, membership.UserId });
            entity.Property(membership => membership.Role).HasMaxLength(32).IsRequired();
            entity.Property(membership => membership.CreatedUtc).IsRequired();
            entity.HasIndex(membership => membership.UserId);
            entity.HasIndex(membership => new { membership.OrganizationId, membership.Role });
            entity.HasOne(membership => membership.Organization)
                .WithMany(organization => organization.Memberships)
                .HasForeignKey(membership => membership.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(membership => membership.User)
                .WithMany(user => user.OrganizationMemberships)
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
