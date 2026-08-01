# Database Design

## Overview

The application uses SQLite through `Microsoft.Data.Sqlite`. `SqliteDataStore` owns schema creation, queries, transactions, audit writes, legacy import, and backup. On every connection it enables foreign keys, WAL journaling, `synchronous=NORMAL`, and a 15-second busy timeout. The connection also uses shared cache and pooling.

Default path: `%ProgramData%\HammerTrackPro2\hammertrack.db`.

Optional workstation override: `%LocalAppData%\HammerTrackPro2\workstation.json`, whose `DatabasePath` may be an absolute or environment-variable-expanded path. Backups are timestamped `.db` files in `Backups` beside the active database.

> Operational assumption requiring validation: a path may point to a network share, but the repository contains no evidence defining or proving a supported shared-filesystem topology. SQLite WAL and network filesystem semantics must be validated for the chosen environment before production use.

## Current schema (version 5)

Version 4 additively creates Consignors. Version 5 creates an Auction from retained legacy Settings values, creates a disabled Legacy / Unassigned consignor when needed, and links legacy lots to both records. Existing columns and Settings are not deleted.

### `Auctions`

`AuctionId` is the primary key. Auction name/date, location, buyer premium, default consignor commission, sales tax, status, and UTC timestamps are persisted. Commercial terms are invariant decimal text and must be read from the current Auction record, never hard-coded. Date and status are indexed.

### `Consignors`

`ConsignorId` is the stable primary key and `ConsignorNumber` is unique. Records include business/contact/address details, Tax ID, default/override commission, notes, Active, PrintCheck, HoldCheck, TaxExempt, and Is1099Required. Name, phone, email, and Active are indexed.

### `Bidders`

- `Id INTEGER PRIMARY KEY AUTOINCREMENT`
- `BidderNumber INTEGER NOT NULL UNIQUE` — application-facing identity
- `FirstName`, `LastName`, `DriversLicense`, `Address`, `City`, `State`, `Zip`, `Phone`, `Email`, `Notes` — non-null text, default empty
- `CreatedUtc`, `UpdatedUtc TEXT NOT NULL` — ISO-8601 UTC
- Indexes: name `(LastName, FirstName)`, driver’s license, and phone

### `Lots`

- `Id INTEGER PRIMARY KEY AUTOINCREMENT`
- `LotNumber INTEGER NOT NULL UNIQUE` — application-facing identity
- `Description`, `Category TEXT NOT NULL DEFAULT ''`
- `StartingBid TEXT NOT NULL DEFAULT '0'` — invariant-culture decimal text
- `Status TEXT NOT NULL DEFAULT 'Available'`
- `AuctionId` -> `Auctions.AuctionId`; `ConsignorId` -> `Consignors.ConsignorId`
- `ReservePrice`, `PassOutMode`, and `LotStatus`; legacy `StartingBid` and `Status` remain
- `CreatedUtc`, `UpdatedUtc TEXT NOT NULL`
- Indexes: description, category, and status
- Ownership/lookup indexes: auction, consignor, and `(AuctionId, LotNumber)`

Observed UI status values are `Available`, `Hold`, and `Sold`; this is not enforced by a database constraint.

### `Sales`

- `Id INTEGER PRIMARY KEY AUTOINCREMENT`
- `LotNumber INTEGER NOT NULL` -> `Lots.LotNumber` (`ON UPDATE CASCADE`, `ON DELETE RESTRICT`)
- `BidderNumber INTEGER NOT NULL` -> `Bidders.BidderNumber` (`ON UPDATE CASCADE`, `ON DELETE RESTRICT`)
- `HammerPrice TEXT NOT NULL DEFAULT '0'` — invariant-culture decimal text
- `SaleUtc TEXT NOT NULL`
- `Paid INTEGER NOT NULL DEFAULT 0` — modeled as boolean; no current cashier workflow updates it
- Indexes: bidder number; lot number is created by the v3 upgrade path (see caveat below)

Multiple rows per lot support pass-out sales. A standard sale marks its lot `Sold`; pass-out recording leaves it open until explicitly closed.

### `AuditLog`

- `Id INTEGER PRIMARY KEY AUTOINCREMENT`
- `EventUtc TEXT NOT NULL`
- `Action TEXT NOT NULL`
- `Details TEXT NOT NULL DEFAULT ''`
- Index: event UTC

The data store writes audit events for bidder, lot, sale, undo, close, and legacy-import mutations. Audit entries are append-only by convention, not by database permissions or triggers.

### `Settings`

- `Key TEXT PRIMARY KEY`
- `Value TEXT NOT NULL DEFAULT ''`

Known keys include `DatabaseSchemaVersion`, `CurrentAuctionId`, retained legacy auction values, clerk/cashier settings, and per-lot notes using `LotNotes:{lotNumber}`.

## Integrity and transaction rules

- Use parameterized SQL exclusively.
- Wrap related writes and their audit entry in one transaction.
- Store UTC timestamps in round-trip ISO-8601 form and decimals as invariant text, matching current behavior.
- Keep `PRAGMA foreign_keys = ON` for every connection.
- Back up before schema upgrades and test both fresh creation and representative upgrades.
- Never commit databases, WAL/SHM files, backups, or workstation path configuration.

## Current migrations and import

Startup creates missing tables and sets schema version `3` if absent. A targeted upgrade detects the older `Sales` definition where `LotNumber` was unique, rebuilds that table without the uniqueness rule, copies data, recreates indexes, and updates the version. If an empty SQLite database sits beside `hammertrack-data.json`, bidder and lot data is imported once and the JSON file is renamed with `.migrated`.

## Known gaps and open decisions

- Fresh schema creation defines only the sales bidder index; the lot index is created only during the legacy pass-out upgrade. Decide whether to add an idempotent index migration.
- Schema versioning is partly declarative and partly based on inspecting table SQL. Adopt ordered, transactional migrations before more schema changes.
- The schema has no Auction/Event table, so settings, lots, bidders, sales, and the latest global undo all share one namespace.
- `Status`, `Paid`, email/phone formats, and positive monetary/number rules are enforced incompletely or only in application code.
- Renumbering a bidder/lot deletes the old row before upsert in current persistence code; define and test behavior when sales reference that row.
- Define privacy, retention, encryption, backup access, restore testing, and audit retention requirements.
- Define concurrency targets and failure recovery for multi-workstation use. In-process locking does not coordinate separate processes.
