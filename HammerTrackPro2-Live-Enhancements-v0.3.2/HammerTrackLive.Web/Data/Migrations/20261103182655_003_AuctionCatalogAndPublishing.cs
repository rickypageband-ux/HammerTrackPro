using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HammerTrackLive.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class _003_AuctionCatalogAndPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Organizations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Organizations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicEmail",
                table: "Organizations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Organizations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Organizations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OnlineAuctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartsUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShippingAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    BuyerPremiumPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    SalesTaxPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxBasis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PaymentInstructions = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    PickupInstructions = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    ShippingInstructions = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineAuctions", x => x.Id);
                    table.CheckConstraint("CK_OnlineAuctions_Dates", "\"EndsUtc\" > \"StartsUtc\"");
                    table.CheckConstraint("CK_OnlineAuctions_Percentages", "\"BuyerPremiumPercent\" >= 0 AND \"BuyerPremiumPercent\" <= 100 AND \"SalesTaxPercent\" >= 0 AND \"SalesTaxPercent\" <= 100");
                    table.CheckConstraint("CK_OnlineAuctions_Status", "\"Status\" IN ('Draft', 'Published', 'Closed')");
                    table.ForeignKey(
                        name: "FK_OnlineAuctions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnlineAuctionTermsSnapshots",
                columns: table => new
                {
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerPremiumPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    SalesTaxPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxBasis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PaymentInstructions = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    PickupInstructions = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    ShippingInstructions = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    CapturedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineAuctionTermsSnapshots", x => x.AuctionId);
                    table.ForeignKey(
                        name: "FK_OnlineAuctionTermsSnapshots_OnlineAuctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "OnlineAuctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnlineLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Condition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartingBid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    BidIncrement = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ReservePrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineLots", x => x.Id);
                    table.CheckConstraint("CK_OnlineLots_LotNumber", "\"LotNumber\" > 0");
                    table.CheckConstraint("CK_OnlineLots_Money", "\"StartingBid\" >= 0 AND \"BidIncrement\" > 0 AND (\"ReservePrice\" IS NULL OR \"ReservePrice\" >= 0)");
                    table.CheckConstraint("CK_OnlineLots_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_OnlineLots_OnlineAuctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "OnlineAuctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnlineLotMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineLotMedia", x => x.Id);
                    table.CheckConstraint("CK_OnlineLotMedia_Kind", "\"Kind\" IN ('Image', 'Document')");
                    table.ForeignKey(
                        name: "FK_OnlineLotMedia_OnlineLots_LotId",
                        column: x => x.LotId,
                        principalTable: "OnlineLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAuctions_OrganizationId_Status_StartsUtc",
                table: "OnlineAuctions",
                columns: new[] { "OrganizationId", "Status", "StartsUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAuctions_Slug",
                table: "OnlineAuctions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAuctions_Status_EndsUtc",
                table: "OnlineAuctions",
                columns: new[] { "Status", "EndsUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLotMedia_LotId_SortOrder",
                table: "OnlineLotMedia",
                columns: new[] { "LotId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLotMedia_StorageKey",
                table: "OnlineLotMedia",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLots_AuctionId_IsActive",
                table: "OnlineLots",
                columns: new[] { "AuctionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLots_AuctionId_LotNumber",
                table: "OnlineLots",
                columns: new[] { "AuctionId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLots_Category",
                table: "OnlineLots",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnlineAuctionTermsSnapshots");

            migrationBuilder.DropTable(
                name: "OnlineLotMedia");

            migrationBuilder.DropTable(
                name: "OnlineLots");

            migrationBuilder.DropTable(
                name: "OnlineAuctions");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PublicEmail",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Organizations");
        }
    }
}
