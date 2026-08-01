# Current Sprint

## Sprint 2 — Auction Setup

Status: implemented; pending final verification/commit.

The sprint delivers functional architecture, persistence, business rules, tests, and an unstyled Auction Setup screen. Cashier, payments, reports, settlements, printing, and UI styling are explicitly out of scope.

## Acceptance criteria

- Create, edit, load, select, close, and duplicate auction configuration.
- Persist unique text `AuctionNumber`, date/time, location, staff, commercial terms, footers, status, and creation date.
- Read buyer premium, consignor commission, and tax from the selected Auction record.
- Closing writes an audit event and locks operational mutations. Reopening is not available; `IAuctionSetupService.ReopenAuction` is only the future authorization hook.
- `AuctionConsignors` links global consignor identities to auctions. “Copy Consignors” copies only links, never master consignor rows.
- `AuctionLots` owns lots by `AuctionId`; `(AuctionId, LotNumber)` is unique so numbers can repeat across auctions.
- Global `Buyers` are linked through auction-specific `AuctionBidders`; bidder numbers are unique only within an auction.
- Current registration lists/counts, dashboard/report-style queries, live auction data, sales, and undo are scoped to the current auction.
- Schema version 8 upgrades existing databases without deleting legacy `Bidders`, `Lots`, Settings, or other fields.
- Solution builds with zero errors/warnings and all tests pass before the feature commit.

## Remaining decisions

- Authorization and roles for a future reopen operation.
- Supported multi-workstation topology and recovery testing.
