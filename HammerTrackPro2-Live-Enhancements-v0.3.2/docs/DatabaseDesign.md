# Database Design

## Overview

HammerTrack Pro uses SQLite through `SqliteDataStore` on the live-auction host only. Connections enable foreign keys, WAL, shared cache, pooling, `synchronous=NORMAL`, and a 15-second busy timeout. The default database is `%ProgramData%\HammerTrackPro\hammertrack.db`; custom paths must remain on a local host drive. The WiX installer marks this data tree permanent, and first-run migration copies missing legacy `HammerTrackPro2` data without overwriting either location.

The supported two-PC topology is `Cashier -> paired HTTPS API -> live-auction host -> local SQLite`. The cashier pins the host certificate fingerprint, sends a high-entropy pairing key, and never receives a database path or SQL access. Cashier writes execute through the same transactional service/data-store path as local writes. A disconnected cashier retries health checks; the live-auction host continues locally.

## Schema version 11

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

`Buyers` is the global person/contact identity keyed by `BuyerId`; it has no bidder number. `AuctionBidders` is the registration junction containing `AuctionBidderId`, `AuctionId`, `BuyerId`, `BidderNumber`, registration notes, and timestamps. Both `(AuctionId, BidderNumber)` and `(AuctionId, BuyerId)` are unique. This allows bidder number 25 in many auctions while keeping one buyer history.

`Sales` stores `AuctionId`, `AuctionLotId`, `AuctionBidderId`, retained `LotNumber`, retained `BidderNumber`, hammer price, sale UTC, and paid state. Foreign keys connect sales to the auction, auction lot, and exact auction registration; indexes cover all join paths.

Multiple sale rows per auction lot preserve pass-out behavior. Sale lookup, latest-sale undo, dashboard totals, activity, and lot counts filter by the current Auction.

### `AuditLog` and `Settings`

AuditLog remains append-only by convention and now includes auction save/select/close and association-copy events. Settings retains legacy values and `CurrentAuctionId`; global commercial values are migration sources only, not runtime configuration.

### `Invoices` and `Payments`

Invoices has an auto-increment `InvoiceId`, unique `AuctionBidderId`, AuctionId, snapshotted premium/tax percentage and tax basis, calculated hammer/premium/tax/total values, status, and UTC timestamps. The operator-facing invoice number is formatted from AuctionNumber and BidderNumber; it is not another database identity.

Payments is append-only for the current feature: `PaymentId`, InvoiceId, method, tendered amount, applied amount, change due, optional reference, and creation UTC. Amounts remain invariant decimal text. Indexes cover invoice joins and payment time. Split tender is represented by multiple Payment rows written in one transaction.

### `ConsignorSettlements` and `ConsignorPayouts`

`ConsignorSettlements` has one row per `(AuctionId, ConsignorId)`, foreign keys to the auction and global consignor, snapshotted commission percentage and calculated hammer/commission/net amounts, status, and UTC timestamps. Commission precedence is positive consignor override, positive consignor default, then current-auction default. Terms remain refreshable until the first payout and are then retained for financial history.

`ConsignorPayouts` is an append-only disbursement ledger containing method (`Check`, `Cash`, or `Ach`), amount, reference, and creation UTC. Payouts are allowed only after auction closure, cannot exceed the settlement balance, and are audited transactionally. Check payouts respect `HoldCheck`; check and ACH require a reference. Indexes cover auction/consignor/status settlement lookup and payout joins/time.

## Migration 10

Migration 10 additively creates `ConsignorSettlements` and `ConsignorPayouts`, their foreign keys and indexes, and advances `DatabaseSchemaVersion` without altering existing invoice, payment, sale, auction, lot, or consignor data. Rollback is restore-from-backup; existing version-9 data remains readable and unchanged apart from the new empty settlement tables.

Migration 11 adds nullable `Consignors.LinkedBuyerId`, its Buyer foreign key and lookup index. Existing rows remain unchanged until saved/linked; no Buyer or Consignor rows are overwritten.

Migration 12 adds current-auction `CreditCardFeePercent` (legacy/new default 3%), invoice fee totals, and per-payment processing fees. Fees apply only to card-paid portions and are retained separately for audit/reporting.

Migration 13 adds the auction-scoped append-only `StaffPayments` ledger with self-references for void and replacement entries plus auction/time and void indexes. Original entries are never edited or deleted.

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
