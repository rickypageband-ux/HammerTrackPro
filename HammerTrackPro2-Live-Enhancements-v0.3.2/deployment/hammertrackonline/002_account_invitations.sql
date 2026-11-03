START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103180118_002_AccountInvitations') THEN
    CREATE TABLE "AccountInvitations" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Email" character varying(256) NOT NULL,
        "NormalizedEmail" character varying(256) NOT NULL,
        "DisplayName" character varying(120) NOT NULL,
        "Role" character varying(32) NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "ExpiresUtc" timestamp with time zone NOT NULL,
        "AcceptedUtc" timestamp with time zone,
        "CreatedUtc" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        CONSTRAINT "PK_AccountInvitations" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_AccountInvitations_Role" CHECK ("Role" IN ('Administrator', 'Staff')),
        CONSTRAINT "FK_AccountInvitations_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AccountInvitations_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103180118_002_AccountInvitations') THEN
    CREATE INDEX "IX_AccountInvitations_CreatedByUserId" ON "AccountInvitations" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103180118_002_AccountInvitations') THEN
    CREATE INDEX "IX_AccountInvitations_OrganizationId_NormalizedEmail_AcceptedU~" ON "AccountInvitations" ("OrganizationId", "NormalizedEmail", "AcceptedUtc", "ExpiresUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103180118_002_AccountInvitations') THEN
    CREATE UNIQUE INDEX "IX_AccountInvitations_TokenHash" ON "AccountInvitations" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103180118_002_AccountInvitations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261103180118_002_AccountInvitations', '8.0.33');
    END IF;
END $EF$;
COMMIT;

