# Clerk search and additional buyers (0.7.60)

Owner-approved scope, September 27, 2026: bidding may be finished, but the auction is still open. This feature does not reopen a formally closed auction.

## Operator workflow

1. In the Retail or Wholesale clerk screen, click **SEARCH SOLD ITEMS** below the live-sale history.
2. Search a description, such as `towels`, or an exact lot number. Blank search lists recent sold items. At most 100 matching combinations are displayed; narrow the search when needed.
3. Select the correct lot, seller, and recorded price. If a lot sold at different prices or deal sizes, select the desired combination explicitly.
4. Enter the registered current-auction bidder number. Retail takes Quantity (default 1). Wholesale takes Deals (default 1), retaining the selected sale's price each and items per deal.
5. Check the displayed hammer total and click **Add Buyer at This Price** or press Enter from the entry fields. The dialog closes after one successful purchase. Reopen it for another purchase.

This sells additional units, not the original buyer's physical purchase. Operators must confirm that more stock is available; HammerTrack has no inventory-quantity enforcement. Original buyers retain their purchases and payments. New purchases appear on the receiving buyer's invoice, including a new balance when that buyer previously paid. Current-auction premium and tax rules continue to apply.

## Implementation and compatibility

`SoldItemSearchForm -> SoldItemSearchViewModel -> IAuctionService -> IDataStore -> SqliteDataStore` keeps presentation separate from rules and SQL. A parameterized current-auction/mode search groups valid sales by lot, stored price, and quantity per deal. It excludes voided transactions and lots outside Sold/Pass-Out status.

The save operation accepts the selected source-sale identifier, auction, mode, new bidder, and count; it does not accept a replacement price. Within one SQLite transaction it rechecks the selected auction and open status, reads price/deal size from the unvoided source, verifies bidder registration, inserts a distinct sale, refreshes the invoice, and appends `SaleRecorded` and `AdditionalBuyerRecorded` audit entries. The latter records both sale identifiers. Source prices, quantities, bidder assignments, payments, and lot status are preserved. The existing invoice-wide derived `Sales.Paid` flag is recalculated if the receiving buyer owes a new balance. A corrected/deleted source or changed auction requires a fresh search. Existing normal clerk entry still refuses a closed sold lot.

No schema change or migration is necessary: existing Sales, Invoices, and AuditLog fields represent the operation. Schema version 20, preserved legacy fields, and the version-3 upgrade path remain unchanged. Existing backup/restore guidance applies; back up before installing an upgrade. Rollback of application binaries does not remove new sale rows, which remain ordinary compatible sales.

Existing paid-sale correction and undo rules still apply. Undo of the newest eligible unpaid additional purchase removes only that new sale and refreshes the invoice; the existing undo behavior returns a lot with remaining sales to Pass-Out. Close Lot again when appropriate. Payments on an invoice continue to prevent the ordinary Undo action; use the audited correction/refund workflow instead.

## Acceptance checks

- Description/lot search, price alternatives, and auction/mode isolation.
- Retail and Wholesale totals, persisted source price, unchanged original purchase, seller ownership, invoice recalculation, and audit events.
- Paid-source/paid-buyer handling without replacing payments.
- Rejection of unknown bidder, nonpositive count, overflow, closed auction, stale source, and changed auction.
- Single-save view-model guard, keyboard entry, and visible search action.
- Disposable-database foreign-key, backup, undo, and existing migration regression tests.

## Open decisions

None for this scope. Formally closed auctions remain locked, as confirmed by the owner.
