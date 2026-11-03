# Database Design

## Overview

HammerTrack Pro uses SQLite through `SqliteDataStore` on the live-auction host only. Connections enable foreign keys, WAL, shared cache, pooling, `synchronous=NORMAL`, and a 15-second busy timeout. The default database is `%ProgramData%\HammerTrackPro\hammertrack.db`; custom paths must remain on a local host drive. The WiX installer marks this data tree permanent, and first-run migration copies missing legacy `HammerTrackPro2` data without overwriting either location.

The supported two-PC topology is `Cashier -> paired HTTPS API -> live-auction host -> local SQLite`. The cashier pins the host certificate fingerprint, sends a high-entropy pairing key, and never receives a database path or SQL access. Cashier writes execute through the same transactional service/data-store path as local writes. A disconnected cashier retries health checks; the live-auction host continues locally.

## Schema version 20

Version 0.7.60 adds a clerk search/additional-buyer operation without a schema change. It inserts an ordinary Sale using the selected unvoided source sale's persisted price and Wholesale deal size, within a transaction that verifies current-auction scope and open status, refreshes the invoice, and appends audit entries linking source and new sale IDs. Original sale amounts/ownership, payments, and lot status are preserved on insert; the receiving buyer's invoice-wide derived Paid flags are recalculated. See [Clerk search and additional buyers](SoldItemSearch.md) for workflow, undo behavior, and compatibility.

### `Auctions`

`AuctionId` is the integer primary key. `AuctionNumber` is a unique text business identifier. The record also stores name, date, time, location, staff, commercial percentages, `SalesTaxBasis`, invoice/receipt footers, status, and retained timestamps. `SalesTaxBasis` is `None`, `HammerOnly`, or `HammerPlusBuyerPremium`.

Commercial terms must always come from the current persisted Auction. Runtime code must not hard-code buyer premium, sales tax, or commission percentages.

### `Consignors` and `AuctionConsignors`

`Consignors` is the global master identity, keyed by `ConsignorId` with unique `ConsignorNumber`. It retains all Sprint 1 business/contact, settlement/tax, commission, notes, active, and timestamp fields, including legacy compatibility columns.

`LinkedBuyerId` is an additive nullable foreign key to the durable global Buyer. The UI calls this identity a Seller while retaining the Consignor schema for compatibility. Saving a seller preflights the current-auction Buyer Number before saving, then ensures an AuctionBidder registration with the same number; conflicting registrations are rejected without creating the seller row. The linked buyer's current-auction unpaid invoice balance is deducted from settlement net proceeds.

`AuctionConsignors(AuctionId, ConsignorId, CreatedDate)` is a junction table with a composite primary key and foreign keys to Auctions and Consignors. Copying consignors between auctions inserts association rows only; it never duplicates master records. `ConsignorId` has a reverse-lookup index.

### `AuctionLots`

`AuctionLotId` is the primary key. Every row requires `AuctionId`, `ConsignorId`, `LotNumber`, description, category, starting bid, reserve price, pass-out mode, lot status/status, and timestamps. Foreign keys use `ON DELETE RESTRICT`. `(AuctionId, LotNumber)` is unique, so the same lot number is valid in separate auctions.

Live quick-add uses this same table and relationship model; it does not create a parallel lot type. An unsold item explicitly passed by the operator uses `Status` and `LotStatus` value `Passed`, writes a `LotPassed` audit entry, and is excluded from automatic next-lot selection. No schema migration is required.

Indexes cover `LotNumber`, `AuctionId`, `ConsignorId`, `(AuctionId, LotNumber)`, description, category, and status. The version-7 migration copies existing `Lots` rows into `AuctionLots`; the legacy `Lots` table and fields remain intact for backward compatibility.

### `Buyers`, `AuctionBidders`, and `Sales`

`Buyers` is the global person/contact identity keyed by `BuyerId`; it has no bidder number. It also retains optional `ResaleLicenseNumber`, `ResaleLicenseState`, and `TaxExempt` values so checkout consistently applies the durable buyer's exemption. `AuctionBidders` is the registration junction containing `AuctionBidderId`, `AuctionId`, `BuyerId`, `BidderNumber`, registration notes, and timestamps. Both `(AuctionId, BidderNumber)` and `(AuctionId, BuyerId)` are unique. This allows bidder number 25 in many auctions while keeping one buyer history.

Buyer first/last name, address, city, state, ZIP, phone, email, and driver's license number are optional global identity fields. Both the main Bidders screen and Cashier Add Buyer workflow can populate them; only the auction-specific Bidder Number is required. These columns have existed since migration 8, so exposing them in Cashier requires no new schema migration.

`Sales` stores `AuctionId`, `AuctionLotId`, `AuctionBidderId`, retained `LotNumber`, retained `BidderNumber`, unit hammer price, quantity, sale UTC, and paid state. Quantity defaults to one for backward compatibility, and financial totals use `Quantity * HammerPrice`. Foreign keys connect sales to the auction, auction lot, and exact auction registration; indexes cover all join paths.

Migration 20 adds `AuctionLots.SaleMode` (`Retail` by default), `Sales.QuantityPerDeal`, and `Sales.Deals`. `Sales.Quantity` remains the authoritative total number of items, calculated as quantity per deal times deals for wholesale pass-out transactions. Existing sales migrate to `QuantityPerDeal=Quantity` and `Deals=1`, preserving all historical totals. The `(AuctionId, SaleMode, LotNumber)` index supports mode-specific live-lot selection. The migration is ordered, transactional, idempotent, and backward compatible with version-3 databases; rollback uses the pre-upgrade database backup.

The live-sale history and correction workspace uses the existing indexed current-auction Sales relationships and migrations 17 and 20; it introduces no schema change. Corrections void the selected original row, insert a linked replacement, refresh affected invoices, and append `SaleCorrected` to the audit log. Wholesale replacements retain `QuantityPerDeal`, `Deals`, and their calculated total `Quantity`.

Migration 16 adds the positive integer `Sales.Quantity` column with a default of `1`, preserving every existing sale and its historical total. Rollback uses the pre-migration database backup; the migration is ordered, transactional, and skipped once `DatabaseSchemaVersion` is 16.

Migration 19 additively adds the Buyer resale-license number, issuing state, and tax-exempt flag with blank/false defaults plus an exemption index. It is ordered, transactional, idempotent, preserves version-3 upgrades, and never changes existing Sale rows. Seller reports obtain bidder traceability from the `Sales.BidderNumber` value already retained with each transaction. Rollback is restoration of the automatic pre-upgrade database backup.

Multiple sale rows per auction lot preserve pass-out behavior. Sale lookup, latest-sale undo, dashboard totals, activity, and lot counts filter by the current Auction.

### `AuditLog` and `Settings`

AuditLog remains append-only by convention and now includes auction save/select/close and association-copy events. Settings retains legacy values and `CurrentAuctionId`; global commercial values are migration sources only, not runtime configuration.

### `Invoices` and `Payments`

Invoices has an auto-increment `InvoiceId`, unique `AuctionBidderId`, AuctionId, snapshotted premium/tax percentage and tax basis, calculated hammer/premium/tax/total values, status, and UTC timestamps. The operator-facing invoice number is formatted from AuctionNumber and BidderNumber; it is not another database identity.

Payments remains append-only: `PaymentId`, InvoiceId, method, tendered amount, applied amount, change due, optional reference, and creation UTC. Refunds is a separate append-only ledger tied to both the Invoice and original Payment, with method, purchase-only amount, required reason, optional reference, and UTC timestamp. Refunds never reverse the original card-processing fee. Amounts remain invariant decimal text. Split tender is represented by multiple Payment rows written in one transaction.

Migration 17 additively marks corrected Sales with `IsVoided`, `VoidedUtc`, and `VoidReason`, and links an optional replacement through `CorrectionForSaleId`. Operational invoices, settlements, dashboards, and reports exclude voided sales while retaining the original row and audit event. Migration 18 creates the indexed Refunds ledger with restrictive foreign keys to Invoices and Payments. Both migrations are ordered, transactional, idempotent, and preserve version-3 upgrades; rollback is restore from the pre-upgrade backup.

### `ConsignorSettlements` and `ConsignorPayouts`

`ConsignorSettlements` has one row per `(AuctionId, ConsignorId)`, foreign keys to the auction and global consignor, snapshotted commission percentage and calculated hammer/commission/net amounts, status, and UTC timestamps. Commission precedence is positive consignor override, positive consignor default, then current-auction default. Terms remain refreshable until the first payout and are then retained for financial history.

`ConsignorPayouts` is an append-only disbursement ledger containing method (`Check`, `Cash`, or `Ach`), amount, reference, and creation UTC. Payouts are allowed only after auction closure, cannot exceed the settlement balance, and are audited transactionally. Check payouts respect `HoldCheck`; check and ACH require a reference. Indexes cover auction/consignor/status settlement lookup and payout joins/time.

## Migration 10

Migration 10 additively creates `ConsignorSettlements` and `ConsignorPayouts`, their foreign keys and indexes, and advances `DatabaseSchemaVersion` without altering existing invoice, payment, sale, auction, lot, or consignor data. Rollback is restore-from-backup; existing version-9 data remains readable and unchanged apart from the new empty settlement tables.

Migration 11 adds nullable `Consignors.LinkedBuyerId`, its Buyer foreign key and lookup index. Existing rows remain unchanged until saved/linked; no Buyer or Consignor rows are overwritten.

Migration 12 adds current-auction `CreditCardFeePercent`, invoice fee totals, and per-payment processing fees. Fees apply only to card-paid portions and are retained separately for audit/reporting.

Migration 13 adds the auction-scoped append-only `StaffPayments` ledger with self-references for void and replacement entries plus auction/time and void indexes. Original entries are never edited or deleted.

Migration 14 additively adds `Auctions.CreditCardFeeFixedAmount`, changes persisted auction card percentages from the superseded 3% rate to 2.65%, and defaults the fixed component to $0.15. The existing percentage field and all invoice/payment history are preserved; only future card payments use the updated current-auction terms. Rollback is restore-from-backup.

Migration 15 additively adds optional `CompanyName`, `CompanyAddress`, `CompanyCity`, `CompanyState`, `CompanyZip`, `CompanyPhone`, and `CompanyEmail` fields to `Auctions`. Existing auctions receive empty values and retain every prior field and relationship. The printed invoice/receipt and End-of-Night report fall back to `AuctionName` when Company Name is blank. Rollback is restore-from-backup.

## Migrations 7 and 8

The ordered transaction:

1. Adds Auction Setup columns and gives legacy auctions stable `LEGACY-{AuctionId}` numbers.
2. Creates and populates `AuctionConsignors` without duplicating consignors.
3. Creates `AuctionLots` and copies existing lot data.
4. Rebuilds Sales with auction/auction-lot ownership while retaining the original lot number.
5. Creates scale-oriented foreign-key and lookup indexes and records schema version 7.

Migration 8 creates Buyers and AuctionBidders, migrates every legacy Bidders row to one global Buyer plus a registration in the current auction, and rebuilds Sales with an `AuctionBidderId` foreign key. The legacy Bidders table and globally unique legacy number column remain untouched for backward compatibility but are no longer used for runtime registration.

Migration 9 adds the Auction sales-tax basis, creates one Invoice for every existing AuctionBidder, calculates totals for historical purchases, creates Payments, and retains all prior tables and fields. `HammerOnly` is the backward-compatible legacy default.

Verification covers a new database, a representative version-3 upgrade, retained legacy lots, repeated lot numbers across auctions, association-only copying, closure audit/locking, and current-auction data loading.

## Open decisions

- Administrator/authentication policy for reopening closed auctions.
- Deletion/renumbering policy once financial history exists.
- Privacy, retention, restore drills, and multi-workstation concurrency targets.

## HammerTrack Online PostgreSQL database

The web platform uses a separate PostgreSQL database and never opens, copies, or shares the HammerTrack Pro desktop SQLite file. Migration `001_InitialIdentityAndOrganizations` creates ASP.NET Core Identity tables, `Organizations`, `OrganizationMemberships`, indexes, foreign keys, and the organization-role check constraint. It seeds only the durable platform roles `PlatformAdministrator` and `Bidder`; it does not seed a password or user account.

`ApplicationUser` is the global web identity. `OrganizationMembership` assigns an existing user to an auction company as either `Administrator` or `Staff`; its primary key is `(OrganizationId, UserId)`. Organization slugs are unique, and active/name plus organization/role and user membership lookup paths are indexed. All creation timestamps are UTC `DateTimeOffset` values.

Migration `002_AccountInvitations` additively creates `AccountInvitations`. Each invitation belongs to one organization and its creating user, stores normalized/display email, display name, organization role, expiry, acceptance time, and only the SHA-256 hash of its random one-time token. Token hashes are unique. The `(OrganizationId, NormalizedEmail, AcceptedUtc, ExpiresUtc)` index supports pending-invitation checks, and the role check constraint permits only `Administrator` or `Staff`. Existing users, organizations, memberships, and Identity records remain unchanged.

Invitation acceptance creates the confirmed user and organization membership in one database transaction and marks the invitation accepted. A seven-day application expiry is enforced in addition to single-use acceptance. Production migration uses the checked-in idempotent SQL script after a PostgreSQL custom-format backup; rollback is application rollback plus database restore if the additive table must be removed.

Migration `003_AuctionCatalogAndPublishing` additively extends `Organizations` with public profile fields and creates `OnlineAuctions`, `OnlineAuctionTermsSnapshots`, `OnlineLots`, and `OnlineLotMedia`. Existing organizations receive blank profile defaults and retain every identity/membership value.

`OnlineAuctions` belongs to one Organization, has a globally unique public slug, UTC dates plus the originating IANA time-zone identifier, lifecycle status, and auction-specific commercial terms. Organization/status/date and public status/end-date indexes support administrative and marketplace views. Date, percentage, and lifecycle check constraints protect invalid state.

`OnlineLots` belongs to one OnlineAuction. `(AuctionId, LotNumber)` is unique, with positive lot/quantity and nonnegative monetary constraints. Auction/active and category indexes support catalog paths. `OnlineLotMedia` belongs to one lot, stores only random storage key and file metadata, and has unique key plus lot/sort indexes; bytes live outside PostgreSQL and the deployment tree.

Publishing validates the complete draft and transactionally creates exactly one `OnlineAuctionTermsSnapshot`, copying the auction's persisted premium, tax, tax basis, payment, pickup, and shipping terms before changing status to Published. Public catalog reads the immutable snapshot. Rollback is application rollback plus restoration of the pre-migration database backup; destructive down-migration is not the production rollback strategy.

Production PostgreSQL 16 listens only on server loopback. The application connection string is stored in `/etc/hammertrackonline/database.env`, readable only by root and loaded by systemd. The migration is deployed as an idempotent SQL script and recorded in `__EFMigrationsHistory`. Rollback is application rollback plus restoration of the pre-migration database backup once the database contains customer data; destructive down-migration is not a production rollback strategy.
