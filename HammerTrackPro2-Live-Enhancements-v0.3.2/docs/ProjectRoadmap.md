# Project Roadmap

## Product intent

HammerTrack Pro 2 is a Windows auction-clerking application designed to keep bidder, lot, and sale entry fast and locally available during a live event. The current implementation is a single desktop executable backed by one SQLite database whose path can be configured per workstation.

## Current baseline (Version 0.5 features, observed in code)

- Bidder registration, editing, search, and deletion
- Lot creation, editing, search, status, and deletion
- Auction Setup creation, editing, selection, closure, and configuration-only duplication
- Global consignors linked to auctions through `AuctionConsignors`
- Auction-owned lots with per-auction lot-number uniqueness
- Persisted per-auction commercial terms; no hard-coded runtime premium/commission/tax values
- Current-auction dashboard counts, gross sales, average sale price, and recent activity
- Current-auction Cashier with persistent invoices, partial/split payments, cash change, and invoice printing
- Append-only purchase refunds and audited sale correction/removal/reassignment, with paid-sale refund enforcement and retained card fees
- Paired two-PC LAN mode with a live-auction HTTPS host and dedicated remote cashier workstation
- Self-contained WiX MSI packaging with persistent ProgramData storage and signed offline per-PC licensing
- Standard and multi-buyer pass-out sale recording
- Last-sale undo, lot closing, queued next lot, per-lot auctioneer notes, fullscreen mode, hotkeys, and confirmation sounds
- SQLite initialization/upgrade, audit log writes, legacy JSON import, WAL mode, and in-app backup
- Per-workstation database-path override

## Proposed phases (not yet approved commitments)

### Phase 1 — Reliability baseline

- Add automated tests for validation, standard/pass-out sales, undo, close-lot behavior, schema initialization/upgrades, and legacy import.
- Define and test supported database deployment: local-only versus a specific shared-network topology.
- Document backup, restore, permission, outage, and recovery procedures.
- Add structured schema migrations rather than relying on table-shape detection alone.
- Resolve known data-integrity and operator-safety decisions listed below.

### Phase 2 — Complete the auction workflow

- Define refund/void authorization, receipt behavior, and external payment processing only when separately approved.
- Extend the established Auction scoping to every future report and financial workflow.
- Add exports and operational reports only after their required fields and accounting rules are approved.

### Phase 3 — Multi-workstation operations

- The initial two-PC host/cashier topology is implemented; production qualification remains in this phase.
- Load/concurrency test the approved topology using representative auction volume and failure cases.
- Add visible connection/database identity and health status for operators.
- Define conflict handling, least-privilege access, data-at-rest/in-transit protection, and centralized backup ownership.
- If SQLite on a share cannot meet reliability goals, evaluate a client/server database behind a service boundary.

### Phase 4 — Distribution and supportability

- Complete production code signing and release notes around the repeatable WiX installer and documented upgrade/rollback procedure.
- Add CI build and automated test checks.
- Define telemetry/logging and support bundles with explicit privacy controls.
- Establish supported Windows versions and release acceptance tests.

## Open product and architecture decisions

1. Which roles may reopen a closed auction? Reopening intentionally awaits authentication/authorization.
2. What network topology, number of simultaneous workstations, latency, and disconnect behavior must be supported?
3. May a bidder or lot with historical sales ever be deleted or renumbered? Foreign keys restrict deletion and cascade number updates, while UI/service behavior needs an explicit policy.
5. Which lot statuses are valid? The model defaults to `Available`, the UI also uses `Hold` and `Sold`, but the database has no constraint.
6. What retention, privacy, and access rules apply to driver’s-license and contact data?
8. Who owns roadmap approval, releases, and production database recovery?

## Roadmap hygiene

Move an item into `CurrentSprint.md` only after scope and acceptance criteria are approved. When functionality ships, update the baseline, database design if applicable, and README in the same change.
