# Database Design

## Overview

HammerTrack Pro uses SQLite through `SqliteDataStore`. Connections enable foreign keys, WAL, shared cache, pooling, `synchronous=NORMAL`, and a 15-second busy timeout. Decimals are invariant-culture `TEXT`; timestamps are UTC ISO-8601. The default database is `%ProgramData%\HammerTrackPro2\hammertrack.db` with an optional workstation path override.

> Open operational decision: a configurable network path does not establish that SQLite/WAL is safe on every network filesystem. The supported topology, workstation count, disconnect behavior, and recovery procedure still require testing.

## Schema version 9

### `Auctions`

`AuctionId` is the integer primary key. `AuctionNumber` is a unique text business identifier. The record also stores name, date, time, location, staff, commercial percentages, `SalesTaxBasis`, invoice/receipt footers, status, and retained timestamps. `SalesTaxBasis` is `None`, `HammerOnly`, or `HammerPlusBuyerPremium`.

Commercial terms must always come from the current persisted Auction. Runtime code must not hard-code buyer premium, sales tax, or commission percentages.

### `Consignors` and `AuctionConsignors`

`Consignors` is the global master identity, keyed by `ConsignorId` with unique `ConsignorNumber`. It retains all Sprint 1 business/contact, settlement/tax, commission, notes, active, and timestamp fields, including legacy compatibility columns.

`AuctionConsignors(AuctionId, ConsignorId, CreatedDate)` is a junction table with a composite primary key and foreign keys to Auctions and Consignors. Copying consignors between auctions inserts association rows only; it never duplicates master records. `ConsignorId` has a reverse-lookup index.

### `AuctionLots`

`AuctionLotId` is the primary key. Every row requires `AuctionId`, `ConsignorId`, `LotNumber`, description, category, starting bid, reserve price, pass-out mode, lot status/status, and timestamps. Foreign keys use `ON DELETE RESTRICT`. `(AuctionId, LotNumber)` is unique, so the same lot number is valid in separate auctions.

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
