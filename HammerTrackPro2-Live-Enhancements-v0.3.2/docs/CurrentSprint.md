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
- Current-auction seller settlements allow append-only payouts before or after auction close, limited to the currently available balance. The UI warns before an early payout because later sales or buyer purchases can change the final balance.
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
- Starting a new bidder clears the bidder grid's visual selection so it cannot imply that a registration is selected for deletion; Delete now gives an actionable prompt when no current-auction bidder is selected.
- Cashier invoice and consignor settlement print actions open a full print preview first; operators can print from the preview toolbar.

## Distribution and licensing extension

- WiX 5 builds a versioned, self-contained x64 MSI for installation on multiple PCs without a separate .NET runtime.
- Install application binaries under `%ProgramFiles%\HammerTrack Pro`; create Desktop and Start Menu shortcuts and register `.htpbackup`.
- Keep `%ProgramData%\HammerTrackPro` permanent across upgrades/uninstall and create `Backups`, `Logs`, and `Settings` with normal-user write access.
- Copy missing legacy `HammerTrackPro2` database/configuration data without overwriting or deleting either copy.
- Require an RSA-signed offline license listing the PC Machine IDs. Keep the private issuer key outside Git and outside the MSI.
- Use a stable MSI UpgradeCode and increasing package versions for in-place upgrades.
- Display the RPDAVID LLC HammerTrack Pro EULA and require acceptance before installation.
- Assign the embedded HammerTrack Pro icon explicitly to Desktop and Start Menu shortcuts; package this correction as upgrade version 0.7.1.
- Require an explicit completed workstation-role selection before startup; the presence of a default, migrated, or incomplete configuration file must not silently select Auction Host. Package this correction as upgrade version 0.7.2.
- Render the Network Information and Backup Database header actions with explicit white text and a high-contrast border. Package this correction as upgrade version 0.7.3.
- Remaining release requirement: sign the executable and MSI with a trusted publisher certificate before broad production distribution.

## Live Auction quick-add extension

- Create a current-auction lot during live operations from an active Consignor Number and description, assigning the next auction-scoped lot number.
- Preserve the separate full lot-entry workflow.
- Make the new lot immediately selectable for bidder and hammer-price entry.
- Provide a distinct Pass Item / Next action that marks an unsold item `Passed`, audits it, and advances; retain existing multi-buyer Pass-Out Mode unchanged.
- Reject missing/inactive consignors, closed auctions, blank descriptions, and passing lots that already have buyer entries.

## Cashier registration extension

- Direct Buyer Number and Consignor Number lookup remains available.
- Register new buyers and consignors from local or LAN Cashier and immediately select the invoice or settlement.
- Host-side validation preserves current-auction registration and rejects duplicate numbers with actionable messages.

## Unpaid buyer review

- Cashier provides an on-demand current-auction list of Buyer Number, name, invoice total, paid amount, and balance due, with an explicit all-paid state.
- Every press reloads host-owned invoice data; LAN Cashier uses the paired API.

## Consignor/Buyer settlement offset

- Consignor save creates or links the same-number current-auction Buyer registration and records a durable `LinkedBuyerId`; conflicting ownership is rejected.
- Settlement detail and printing show gross sales, commission/fees, linked buyer unpaid-purchase deduction, adjusted net, payouts, and remaining balance.
- Operator-facing screens and printed settlement documents use Seller terminology while internal Consignor schema/type names remain unchanged for compatibility.
- A new Seller Number is preflighted against current-auction Buyer Numbers before the seller row is saved. A conflict stops the save with a clear request to choose another number; otherwise the matching Buyer registration is created and linked automatically.
- The printed deduction label is shortened to `Buyer balance deducted` so it cannot overlap the amount column.

## Credit-card processing fee

- Current Auction configures `CreditCardFeePercent` (default 3%). Card payments add and pay the fee on only their card portion; cash/check portions have zero fee.
- Invoice UI, persistence, and printout show processing fees separately.

## End-of-night settlement

- Record clerk, auctioneer, floor-help, and kitchen-help payments in an append-only current-auction ledger.
- Corrections require a reason and append linked void/replacement entries; originals remain intact and audited.
- Current-data report and print preview reconcile hammer, premiums, card fees, sales tax, invoice paid/unpaid, consignor commissions/offsets/payouts, staff payments, and net cash movement without counting internal offsets as cash.

## Invoice condition of sale

- Every local or LAN Cashier invoice preview/print includes the approved Condition of Sale – Disclaimer of Warranties with its exact heading and two-paragraph wording.
