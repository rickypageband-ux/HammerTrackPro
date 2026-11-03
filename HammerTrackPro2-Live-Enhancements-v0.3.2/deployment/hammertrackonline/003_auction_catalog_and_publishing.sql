START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "Address" character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "City" character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "Description" character varying(2000) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "Phone" character varying(30) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "PostalCode" character varying(20) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "PublicEmail" character varying(256) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "State" character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    ALTER TABLE "Organizations" ADD "Website" character varying(300) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE TABLE "OnlineAuctions" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Slug" character varying(120) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Description" character varying(5000) NOT NULL,
        "Location" character varying(300) NOT NULL,
        "TimeZoneId" character varying(100) NOT NULL,
        "StartsUtc" timestamp with time zone NOT NULL,
        "EndsUtc" timestamp with time zone NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ShippingAvailable" boolean NOT NULL,
        "BuyerPremiumPercent" numeric(5,2) NOT NULL,
        "SalesTaxPercent" numeric(5,2) NOT NULL,
        "TaxBasis" character varying(40) NOT NULL,
        "PaymentInstructions" character varying(3000) NOT NULL,
        "PickupInstructions" character varying(3000) NOT NULL,
        "ShippingInstructions" character varying(3000) NOT NULL,
        "CreatedUtc" timestamp with time zone NOT NULL,
        "UpdatedUtc" timestamp with time zone NOT NULL,
        "PublishedUtc" timestamp with time zone,
        CONSTRAINT "PK_OnlineAuctions" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_OnlineAuctions_Dates" CHECK ("EndsUtc" > "StartsUtc"),
        CONSTRAINT "CK_OnlineAuctions_Percentages" CHECK ("BuyerPremiumPercent" >= 0 AND "BuyerPremiumPercent" <= 100 AND "SalesTaxPercent" >= 0 AND "SalesTaxPercent" <= 100),
        CONSTRAINT "CK_OnlineAuctions_Status" CHECK ("Status" IN ('Draft', 'Published', 'Closed')),
        CONSTRAINT "FK_OnlineAuctions_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE TABLE "OnlineAuctionTermsSnapshots" (
        "AuctionId" uuid NOT NULL,
        "BuyerPremiumPercent" numeric(5,2) NOT NULL,
        "SalesTaxPercent" numeric(5,2) NOT NULL,
        "TaxBasis" character varying(40) NOT NULL,
        "PaymentInstructions" character varying(3000) NOT NULL,
        "PickupInstructions" character varying(3000) NOT NULL,
        "ShippingInstructions" character varying(3000) NOT NULL,
        "CapturedUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OnlineAuctionTermsSnapshots" PRIMARY KEY ("AuctionId"),
        CONSTRAINT "FK_OnlineAuctionTermsSnapshots_OnlineAuctions_AuctionId" FOREIGN KEY ("AuctionId") REFERENCES "OnlineAuctions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE TABLE "OnlineLots" (
        "Id" uuid NOT NULL,
        "AuctionId" uuid NOT NULL,
        "LotNumber" integer NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Description" character varying(5000) NOT NULL,
        "Category" character varying(100) NOT NULL,
        "Condition" character varying(100) NOT NULL,
        "StartingBid" numeric(12,2) NOT NULL,
        "BidIncrement" numeric(12,2) NOT NULL,
        "ReservePrice" numeric(12,2),
        "Quantity" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedUtc" timestamp with time zone NOT NULL,
        "UpdatedUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OnlineLots" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_OnlineLots_LotNumber" CHECK ("LotNumber" > 0),
        CONSTRAINT "CK_OnlineLots_Money" CHECK ("StartingBid" >= 0 AND "BidIncrement" > 0 AND ("ReservePrice" IS NULL OR "ReservePrice" >= 0)),
        CONSTRAINT "CK_OnlineLots_Quantity" CHECK ("Quantity" > 0),
        CONSTRAINT "FK_OnlineLots_OnlineAuctions_AuctionId" FOREIGN KEY ("AuctionId") REFERENCES "OnlineAuctions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE TABLE "OnlineLotMedia" (
        "Id" uuid NOT NULL,
        "LotId" uuid NOT NULL,
        "StorageKey" character varying(500) NOT NULL,
        "OriginalFileName" character varying(255) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "Kind" character varying(20) NOT NULL,
        "SortOrder" integer NOT NULL,
        "CreatedUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OnlineLotMedia" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_OnlineLotMedia_Kind" CHECK ("Kind" IN ('Image', 'Document')),
        CONSTRAINT "FK_OnlineLotMedia_OnlineLots_LotId" FOREIGN KEY ("LotId") REFERENCES "OnlineLots" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE INDEX "IX_OnlineAuctions_OrganizationId_Status_StartsUtc" ON "OnlineAuctions" ("OrganizationId", "Status", "StartsUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE UNIQUE INDEX "IX_OnlineAuctions_Slug" ON "OnlineAuctions" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE INDEX "IX_OnlineAuctions_Status_EndsUtc" ON "OnlineAuctions" ("Status", "EndsUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE INDEX "IX_OnlineLotMedia_LotId_SortOrder" ON "OnlineLotMedia" ("LotId", "SortOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE UNIQUE INDEX "IX_OnlineLotMedia_StorageKey" ON "OnlineLotMedia" ("StorageKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE INDEX "IX_OnlineLots_AuctionId_IsActive" ON "OnlineLots" ("AuctionId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE UNIQUE INDEX "IX_OnlineLots_AuctionId_LotNumber" ON "OnlineLots" ("AuctionId", "LotNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    CREATE INDEX "IX_OnlineLots_Category" ON "OnlineLots" ("Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103182655_003_AuctionCatalogAndPublishing') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261103182655_003_AuctionCatalogAndPublishing', '8.0.33');
    END IF;
END $EF$;
COMMIT;
