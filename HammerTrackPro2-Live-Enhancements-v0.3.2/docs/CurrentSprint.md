# Current Sprint

## Cashier module

Status: Cashier is implemented. A two-workstation LAN deployment slice is now implemented and pending final verification/feature commit.

## Scope and acceptance criteria

- Look up a Buyer by the current auction's `BidderNumber` registration.
- Display all purchases for that AuctionBidder and no purchases from other auctions.
- Maintain one persistent Invoice per AuctionBidder with a stable internal `InvoiceId` and display number `{AuctionNumber}-{BidderNumber:D4}`.
- Calculate buyer premium from the Invoice's persisted Auction terms.
- Calculate sales tax using the Auction's explicit basis: none, hammer only, or hammer plus buyer premium.
- Record append-only cash, check, and card Payment rows; a split payment is multiple rows in one cashier submission.
- Allow partial payments. Reject non-cash overpayment. Apply cash only to the remaining balance and record change due.
- Update Invoice status to `Unpaid`, `Partially Paid`, or `Paid`; synchronize the retained legacy `Sales.Paid` flag only when the complete invoice is paid.
- Print an itemized invoice through the Windows print dialog, including auction/buyer identity, purchases, premium, tax, payments, balance, status, and invoice footer.
- Closed auctions allow invoice viewing/reprinting but reject new payments.
- Preserve legacy tables/fields and upgrade existing databases through schema version 9.
- Add tests for tax modes, lookup, calculations, split/partial payment behavior, change, overpayment rejection, migration, and financial guards.

## Out of scope

- Refunds, voids, card processing gateways, receipt printing, user authentication, reports, settlements, check printing, and UI styling.

## Two-workstation deployment acceptance

- Live-auction PC is the only SQLite/WAL database owner and hosts the Cashier API on the private LAN.
- Cashier workstation runs a dedicated remote Cashier screen and never opens a local/shared auction database.
- HTTPS, certificate fingerprint pinning, and a high-entropy pairing key protect the connection.
- One-time Host/Cashier setup is persisted per workstation; the host displays computer-name and IPv4 connection addresses.
- Cashier displays connection state and retries health checks after disconnects.
- Remote invoice reads and payment writes retain current-auction scoping, validation, transactions, audit entries, and invoice status behavior.
- UNC and detected mapped-network database paths are rejected.
- Integration tests cover a host/client invoice and payment round trip plus invalid-pairing rejection.

## Remaining decisions

- Refund/void authorization and audit workflow.
- Whether payment references require method-specific formatting or validation.
- Supported multi-workstation topology and recovery testing.

## Post-sprint usability correction

- Lot entry refreshes its consignor list immediately after Consignor Management changes and displays both active and inactive consignors with number, name, and inactive state.
- Auction Information no longer clips Seller Commission or lower fields at common window sizes.
- Auction and consignor properties are grouped into readable categories, and affected grids/forms use clearer spacing, headers, and selection colors.
- Bidder, consignor, and lot editor panels open at a usable width and cannot collapse below the data-entry minimum, eliminating the need to drag the divider open.
