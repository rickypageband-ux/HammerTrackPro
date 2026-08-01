# Current Sprint

## Cashier module

Status: Cashier and the approved closed-auction consignor payout extension are implemented.

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
- Closed auctions enable current-auction consignor settlements. Open auctions reject consignor payouts.
- Calculate consignor commission using `CommissionOverridePercent` when positive, then consignor `DefaultCommissionPercent`, then the current Auction default.
- Record append-only check, cash, and ACH/manual electronic payouts with partial payout support, balance enforcement, audit entries, and printable settlement statements.
- `HoldCheck` blocks check payouts; check and ACH payouts require a reference.
- Preserve legacy tables/fields and upgrade existing databases through schema version 10.
- Add tests for tax modes, lookup, calculations, split/partial payment behavior, change, overpayment rejection, migration, and financial guards.

## Out of scope

- Refunds, voids, card processing gateways, receipt printing, user authentication, reports, automated check stock printing, payout reversals/voids, and UI styling.

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

## Distribution and licensing extension

- WiX 5 builds a versioned, self-contained x64 MSI for installation on multiple PCs without a separate .NET runtime.
- Install application binaries under `%ProgramFiles%\HammerTrack Pro`; create Desktop and Start Menu shortcuts and register `.htpbackup`.
- Keep `%ProgramData%\HammerTrackPro` permanent across upgrades/uninstall and create `Backups`, `Logs`, and `Settings` with normal-user write access.
- Copy missing legacy `HammerTrackPro2` database/configuration data without overwriting or deleting either copy.
- Require an RSA-signed offline license listing the PC Machine IDs. Keep the private issuer key outside Git and outside the MSI.
- Use a stable MSI UpgradeCode and increasing package versions for in-place upgrades.
- Remaining release requirement: sign the executable and MSI with a trusted publisher certificate before broad production distribution.
