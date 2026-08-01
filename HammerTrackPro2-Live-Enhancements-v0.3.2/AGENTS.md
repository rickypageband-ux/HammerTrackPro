# AGENTS.md

These instructions apply to the entire repository. This file and `docs/` are the project memory: update them when an approved change alters behavior, architecture, schema, workflow, or priorities.

## Binding HammerTrack Pro rules

These rules are mandatory for all future work and override less-specific guidance elsewhere in this repository:

1. **Never break a compiling build.** Begin from a compiling baseline whenever the environment permits, keep changes buildable, and do not hand off a feature with compile errors.
2. **Always use MVVM.** New features and touched presentation logic must use Model-View-ViewModel separation. The existing forms are code-driven WinForms and are documented as technical debt, not as a precedent to extend. Introduce MVVM incrementally without unrelated rewrites.
3. **Every feature must have unit tests.** A feature is incomplete until its business behavior and relevant persistence behavior have automated tests.
4. **Never delete existing database fields.** Preserve all existing columns and their data, including fields no longer shown in the UI.
5. **Always create migrations.** Every schema change must be represented by an explicit, ordered, idempotent migration; do not rely only on fresh-schema edits or ad hoc table-shape detection.
6. **Every feature gets its own commit.** Keep each feature isolated in a dedicated commit containing its implementation, tests, migration, and documentation. Do not mix unrelated work into that commit.
7. **All database changes must be backward compatible.** Existing version-3 databases must open and retain their data; additive changes need safe defaults, and older application/data interactions must be considered and documented.
8. **Never hard-code per-auction commercial terms.** Buyer premium, sales tax, default consignor commission, and similar values must come from the current persisted `Auction` record (for example, `CurrentAuction.BuyerPremiumPercent`). A safe default is permitted only while migrating legacy data; runtime calculations must not embed a percentage.
9. **Design core relationships for durable scale.** Assume hundreds of auctions, thousands of consignors, tens of thousands of buyers, and hundreds of thousands of lots. Do not prematurely optimize, but require foreign keys for Auction/Consignor/Lot ownership and indexes for identifiers, search fields, foreign-key joins, auction status/date, and `(AuctionId, LotNumber)` lookup paths.
10. **Keep buyers global and registrations auction-specific.** `Buyer` is the durable person identity. `AuctionBidder` assigns a `BidderNumber` within one auction; uniqueness is `(AuctionId, BidderNumber)`, never global. Do not duplicate a Buyer merely to register that person in another auction.
11. **Scope operational views and reports to Current Auction.** Registration lists/counts, lots, sales, dashboard totals, undo, and all future reports must resolve through the selected Auction unless a screen is explicitly a cross-auction history view.
12. **Keep cashier terms and records auditable.** Invoice premium and tax rules come from the persisted Auction. Tax basis is explicitly `None`, `HammerOnly`, or `HammerPlusBuyerPremium`. Keep one Invoice per AuctionBidder and append Payments separately; do not overwrite payment history or infer financial terms from global settings.

If a requested change conflicts with one of these rules, stop and obtain a maintainer decision rather than weakening the rule silently.

## Before changing code

1. Read `README.md`, `docs/CurrentSprint.md`, and the relevant roadmap/database/standards document.
2. Inspect the existing implementation; do not treat proposed roadmap items as shipped behavior.
3. State assumptions when the repository does not establish a fact. Record decisions that still need an owner under “Open decisions” in the relevant document.
4. Keep work scoped. Do not silently add product features, dependencies, schema changes, or deployment assumptions.
5. Follow `docs/ProductBacklog.md` release boundaries. Do not pull later-version work into the current sprint without explicit approval, and do not guess when an architectural decision is unclear.

## Architectural guardrails

- Preserve the dependency direction: `Forms -> IAuctionService -> IDataStore -> SQLite`.
- Put business validation and auction rules in `AuctionService`, persistence in `SqliteDataStore`, and presentation/event wiring in forms.
- Keep interfaces aligned when service operations change. Favor focused methods over exposing connections or SQL to the UI.
- Treat bidder identity data, sale records, audit history, database paths, and backups as sensitive operational data. Never commit live `.db`, `.db-wal`, `.db-shm`, or workstation configuration files.
- Keep all persisted timestamps in UTC ISO-8601 format; convert only for display.
- Preserve decimal values without binary floating-point conversion. Existing monetary columns are SQLite `TEXT` and are parsed/formatted with invariant culture.
- Enable and respect SQLite foreign keys. Use parameterized commands and transactions for multi-statement mutations.
- Schema upgrades must be idempotent, preserve existing data, update `DatabaseSchemaVersion`, and have a documented backup/rollback approach.
- Do not claim network-share safety without a tested concurrency and failure-recovery plan.

## Implementation conventions

- Target .NET 8 Windows Forms and keep nullable reference types enabled.
- Follow normal C# naming: PascalCase for types/methods/properties, `_camelCase` for private fields, and descriptive names over abbreviations.
- Prefer file-scoped namespaces, `sealed` classes where inheritance is not intended, guard clauses, `using` disposal, and async only when the full call path benefits.
- Keep user-facing errors actionable and avoid exposing database internals or personal data unnecessarily.
- Add tests for new business rules and data-store behavior once a test project exists; until then, document manual verification performed.
- Avoid hand-editing generated `bin/`, `obj/`, or `.vs/` content.

See `docs/CodingStandards.md` for fuller standards.

## Verification

For code changes, at minimum run:

```powershell
dotnet build .\HammerTrackPro2.sln
```

Also exercise the affected workflow with a disposable database. For persistence changes, verify a new database and an upgraded representative database, foreign-key behavior, audit entries, undo behavior where applicable, and backup creation. Never test destructive operations against production data.

## Documentation discipline

- `README.md`: shipped capabilities and setup.
- `docs/ProjectRoadmap.md`: approved direction, proposed phases, and open product decisions.
- `docs/DatabaseDesign.md`: current schema plus explicit migration/concurrency notes.
- `docs/CodingStandards.md`: durable engineering rules.
- `docs/CurrentSprint.md`: only the immediate, owner-approved work and acceptance criteria.
- `docs/ProductBacklog.md`: approved release sequence and scope boundaries.

If no owner has approved sprint scope, label it as a proposed baseline rather than inventing dates, owners, or commitments.
