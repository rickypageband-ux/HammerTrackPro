using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    public DbSet<AccountInvitation> AccountInvitations => Set<AccountInvitation>();
    public DbSet<OnlineAuction> OnlineAuctions => Set<OnlineAuction>();
    public DbSet<OnlineAuctionTermsSnapshot> OnlineAuctionTermsSnapshots => Set<OnlineAuctionTermsSnapshot>();
    public DbSet<OnlineLot> OnlineLots => Set<OnlineLot>();
    public DbSet<OnlineLotMedia> OnlineLotMedia => Set<OnlineLotMedia>();

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
            entity.Property(organization => organization.Address).HasMaxLength(200).IsRequired();
            entity.Property(organization => organization.City).HasMaxLength(100).IsRequired();
            entity.Property(organization => organization.State).HasMaxLength(50).IsRequired();
            entity.Property(organization => organization.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(organization => organization.Phone).HasMaxLength(30).IsRequired();
            entity.Property(organization => organization.PublicEmail).HasMaxLength(256).IsRequired();
            entity.Property(organization => organization.Website).HasMaxLength(300).IsRequired();
            entity.Property(organization => organization.Description).HasMaxLength(2000).IsRequired();
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

        builder.Entity<AccountInvitation>(entity =>
        {
            entity.ToTable("AccountInvitations", table =>
                table.HasCheckConstraint(
                    "CK_AccountInvitations_Role",
                    "\"Role\" IN ('Administrator', 'Staff')"));
            entity.HasKey(invitation => invitation.Id);
            entity.Property(invitation => invitation.Email).HasMaxLength(256).IsRequired();
            entity.Property(invitation => invitation.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(invitation => invitation.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(invitation => invitation.Role).HasMaxLength(32).IsRequired();
            entity.Property(invitation => invitation.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(invitation => invitation.ExpiresUtc).IsRequired();
            entity.Property(invitation => invitation.CreatedUtc).IsRequired();
            entity.HasIndex(invitation => invitation.TokenHash).IsUnique();
            entity.HasIndex(invitation => new
            {
                invitation.OrganizationId,
                invitation.NormalizedEmail,
                invitation.AcceptedUtc,
                invitation.ExpiresUtc
            });
            entity.HasOne(invitation => invitation.Organization)
                .WithMany(organization => organization.Invitations)
                .HasForeignKey(invitation => invitation.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(invitation => invitation.CreatedByUser)
                .WithMany()
                .HasForeignKey(invitation => invitation.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OnlineAuction>(entity =>
        {
            entity.ToTable("OnlineAuctions", table =>
            {
                table.HasCheckConstraint("CK_OnlineAuctions_Status", "\"Status\" IN ('Draft', 'Published', 'Closed')");
                table.HasCheckConstraint("CK_OnlineAuctions_Dates", "\"EndsUtc\" > \"StartsUtc\"");
                table.HasCheckConstraint("CK_OnlineAuctions_Percentages", "\"BuyerPremiumPercent\" >= 0 AND \"BuyerPremiumPercent\" <= 100 AND \"SalesTaxPercent\" >= 0 AND \"SalesTaxPercent\" <= 100");
            });
            entity.HasKey(auction => auction.Id);
            entity.Property(auction => auction.Slug).HasMaxLength(120).IsRequired();
            entity.Property(auction => auction.Title).HasMaxLength(200).IsRequired();
            entity.Property(auction => auction.Description).HasMaxLength(5000).IsRequired();
            entity.Property(auction => auction.Location).HasMaxLength(300).IsRequired();
            entity.Property(auction => auction.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(auction => auction.Status).HasMaxLength(20).IsRequired();
            entity.Property(auction => auction.BuyerPremiumPercent).HasPrecision(5, 2);
            entity.Property(auction => auction.SalesTaxPercent).HasPrecision(5, 2);
            entity.Property(auction => auction.TaxBasis).HasMaxLength(40).IsRequired();
            entity.Property(auction => auction.PaymentInstructions).HasMaxLength(3000).IsRequired();
            entity.Property(auction => auction.PickupInstructions).HasMaxLength(3000).IsRequired();
            entity.Property(auction => auction.ShippingInstructions).HasMaxLength(3000).IsRequired();
            entity.HasIndex(auction => auction.Slug).IsUnique();
            entity.HasIndex(auction => new { auction.OrganizationId, auction.Status, auction.StartsUtc });
            entity.HasIndex(auction => new { auction.Status, auction.EndsUtc });
            entity.HasOne(auction => auction.Organization)
                .WithMany(organization => organization.Auctions)
                .HasForeignKey(auction => auction.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OnlineAuctionTermsSnapshot>(entity =>
        {
            entity.ToTable("OnlineAuctionTermsSnapshots");
            entity.HasKey(terms => terms.AuctionId);
            entity.Property(terms => terms.BuyerPremiumPercent).HasPrecision(5, 2);
            entity.Property(terms => terms.SalesTaxPercent).HasPrecision(5, 2);
            entity.Property(terms => terms.TaxBasis).HasMaxLength(40).IsRequired();
            entity.Property(terms => terms.PaymentInstructions).HasMaxLength(3000).IsRequired();
            entity.Property(terms => terms.PickupInstructions).HasMaxLength(3000).IsRequired();
            entity.Property(terms => terms.ShippingInstructions).HasMaxLength(3000).IsRequired();
            entity.HasOne(terms => terms.Auction)
                .WithOne(auction => auction.TermsSnapshot)
                .HasForeignKey<OnlineAuctionTermsSnapshot>(terms => terms.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OnlineLot>(entity =>
        {
            entity.ToTable("OnlineLots", table =>
            {
                table.HasCheckConstraint("CK_OnlineLots_LotNumber", "\"LotNumber\" > 0");
                table.HasCheckConstraint("CK_OnlineLots_Quantity", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_OnlineLots_Money", "\"StartingBid\" >= 0 AND \"BidIncrement\" > 0 AND (\"ReservePrice\" IS NULL OR \"ReservePrice\" >= 0)");
            });
            entity.HasKey(lot => lot.Id);
            entity.Property(lot => lot.Title).HasMaxLength(200).IsRequired();
            entity.Property(lot => lot.Description).HasMaxLength(5000).IsRequired();
            entity.Property(lot => lot.Category).HasMaxLength(100).IsRequired();
            entity.Property(lot => lot.Condition).HasMaxLength(100).IsRequired();
            entity.Property(lot => lot.StartingBid).HasPrecision(12, 2);
            entity.Property(lot => lot.BidIncrement).HasPrecision(12, 2);
            entity.Property(lot => lot.ReservePrice).HasPrecision(12, 2);
            entity.HasIndex(lot => new { lot.AuctionId, lot.LotNumber }).IsUnique();
            entity.HasIndex(lot => new { lot.AuctionId, lot.IsActive });
            entity.HasIndex(lot => lot.Category);
            entity.HasOne(lot => lot.Auction)
                .WithMany(auction => auction.Lots)
                .HasForeignKey(lot => lot.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OnlineLotMedia>(entity =>
        {
            entity.ToTable("OnlineLotMedia", table =>
                table.HasCheckConstraint("CK_OnlineLotMedia_Kind", "\"Kind\" IN ('Image', 'Document')"));
            entity.HasKey(media => media.Id);
            entity.Property(media => media.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(media => media.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(media => media.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(media => media.Kind).HasMaxLength(20).IsRequired();
            entity.HasIndex(media => media.StorageKey).IsUnique();
            entity.HasIndex(media => new { media.LotId, media.SortOrder });
            entity.HasOne(media => media.Lot)
                .WithMany(lot => lot.Media)
                .HasForeignKey(media => media.LotId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
