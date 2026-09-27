# Current Sprint

## Professional buyer and seller print documents

- Prevent the buyer invoice Quantity header from clipping by widening and separating all numeric columns.
- Apply the existing branded company header to seller settlements and seller item summaries, including company name, address, phone, email, auction identity, seller information, and document number.
- Use consistent section bars, alternating item rows, aligned quantity/price/total columns, payout history, settlement totals, print timestamp, and HammerTrack Pro footer.
- Preserve all invoice and settlement calculations and persistence behavior.
- Package this owner-approved print-layout update as version 0.7.50.

## Software license and ownership terms

- Replace the installer agreement with the owner-approved eight-section Hammer Track Pro Software License and Ownership Terms without adding or removing substantive provisions.
- Continue requiring explicit acceptance in the WiX installation workflow.
- Package the agreement update as version 0.7.49.

## Wholesale Machine ID copy action

- Show the Wholesale activation Machine ID in a read-only selectable field with a one-click Copy Machine ID button.
- Preserve all Wholesale activation, live-entry, pass-out, calculation, persistence, invoice, and reporting behavior unchanged.
- Package this owner-approved convenience update as version 0.7.48.

## Self-hosted product website

- Rebuild the HammerTrack Pro marketing site as a portable PHP site for owner-managed Ubuntu/Plesk hosting.
- Apply the owner-provided HammerTrack Pro logo and its black, white, and red visual identity across the home and pricing pages.
- Publish a dedicated pricing page for Auction Software ($999), Whole Package ($2,500), Wholesale Auction upgrade ($299), Yearly Updates ($199), Hourly Support ($100/hour), and upgrades or feature requests quoted per request.
- Define the Whole Package as two laptops, one laser printer, one NETUM scanner, one network router, on-site South Carolina setup, and one hour of training; disclose that service, setup, and training outside South Carolina may require additional fees.
- Present the licensed Wholesale Auction module as a one-time $299 upgrade and accurately describe its machine-bound offline activation.
- Support a configurable hosted checkout URL with a sales-request fallback; do not claim payment completion when no processor is configured.
- Keep all license-signing secrets and issuer tooling off the web server.
- Provide a health endpoint and Plesk deployment, HTTPS, mail-delivery, and uptime-monitoring instructions.
- Send website requests through authenticated IONOS SMTP using encrypted port 465, keep the mailbox password in a browser-blocked private configuration, deliver to `Info@rpdavid.com`, and use the customer's validated email as Reply-To.

## Offline customer user manual

- Provide a professional, version-matched PDF covering installation, licensing, auction setup, retail and wholesale live entry, checkout, settlements, reports, administration, troubleshooting, and FAQs.
- Use actual application screenshots with numbered callouts for the primary workflows.
- Add a dashboard Help / User Manual action and package the PDF beside the installed application for offline access.
- Package this owner-approved feature as upgrade version 0.7.47.
- Correct the 0.7.47 Wholesale screen startup so restoring an existing Pass-Out lot never calls `BeginInvoke` before the window exists; defer bidder focus until the form is shown.

## Wholesale Auction licensed module

- Rename the dashboard live-entry action to Retail Auction and keep its existing behavior unchanged.
- Keep Wholesale Auction visible as a separately activated module using a signed, machine-bound key; never package the private issuer key.
- Standard wholesale records price each times quantity. Wholesale Pass-Out locks seller, lot, description, hammer price, and quantity per deal, then repeats Bidder Number, Deals, Enter.
- Store quantity per deal and deals separately while retaining total item quantity in the existing `Sales.Quantity` field so invoices, settlements, refunds, dashboards, and reports remain accurate.
- Migration 20 additively introduces `Sales.QuantityPerDeal`, `Sales.Deals`, and `AuctionLots.SaleMode`, migrates legacy sales as one deal, and adds the auction/mode/lot lookup index.
- Package this owner-approved feature as upgrade version 0.7.46.

## Pass-out clerk workflow

- While Pass-Out Mode is active, Seller, Lot Number, Item Description, and Hammer Price remain locked.
- Keyboard entry is Bidder Number, Tab, Quantity, Enter; after recording, focus returns to Bidder Number for the next buyer.
- Create Lot, Pass Item, and next-lot shortcuts cannot advance away from an active pass-out lot.
- Close Lot is the deliberate action that unlocks the fixed fields and advances to the next lot.
- Package this workflow correction as upgrade version 0.7.45.

## Buyer tax exemption and seller bidder traceability

- Buyer registration provides optional Resale License / Tax Exemption Number, State, and Tax Exempt fields on both registration entry points.
- Tax-exempt status remains on the durable global Buyer and automatically suppresses sales tax when invoices are recalculated or checked out.
- Migration 19 adds the three backward-compatible Buyer fields with safe blank/false defaults and an exemption lookup index.
- Seller settlement lines show Lot #, Description, Bidder #, Hammer Price, Quantity, and Total on screen and in both seller print formats.
- Bidder traceability uses the bidder number already retained on each completed Sale; no sale history is rewritten.
- Package these changes as upgrade version 0.7.44.

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
- Seller Management matches the branded Cashier workspace with large uniform Add/Save/Delete actions, full-screen/F11 support, a focused directory that keeps Tax ID out of the list view, seller counts, improved search guidance, and explicit add/edit/save status feedback.
- Package the Seller Management usability and branding update as upgrade version 0.7.15.
- Auction Setup matches the branded Cashier and Seller Management workspaces with full-screen/F11 support, large uniform actions, a clear auction directory/current-auction indicator, duplication guidance, and explicit status feedback.
- Live Auction action buttons span the complete entry card in five equal fixed-height columns, preventing wrapping or clipping at supported window sizes and Windows display scaling; the form provides scrolling if vertical space is constrained.
- Live Auction constrains that five-button action bar to a compact 58-pixel height and gives CREATE LOT a 150-pixel minimum width, preventing full-screen vertical stretching and Quick Add text clipping.
- Package the Live Auction button-sizing correction as upgrade version 0.7.17.
- Duplicating an auction carries forward both persisted credit-card fee components along with the other auction commercial terms.
- Package the Auction Setup redesign and Live Auction action-button layout correction as upgrade version 0.7.16.

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
- Package the Seller workflow and Live Auction entry corrections as upgrade version 0.7.7.
- Package the linked-buyer conflict-message correction as upgrade version 0.7.8.

## Live Auction quick-add extension

- Create a current-auction lot during live operations from an active Consignor Number and description, assigning the next auction-scoped lot number.
- Preserve the separate full lot-entry workflow.
- Make the new lot immediately selectable for bidder and hammer-price entry.
- Keep the quick-add description visible while that lot is active; clear it only when the operator advances or passes to the next item.
- Provide a distinct Pass Item / Next action that marks an unsold item `Passed`, audits it, and advances; retain existing multi-buyer Pass-Out Mode unchanged.
- Reject missing/inactive consignors, closed auctions, blank descriptions, and passing lots that already have buyer entries.

## Cashier registration extension

- Direct Buyer Number and Consignor Number lookup remains available.
- Register new buyers and consignors from local or LAN Cashier and immediately select the invoice or settlement.
- Host-side validation preserves current-auction registration and rejects duplicate numbers with actionable messages.
- The Add Buyer window displays a number-sorted list of every bidder registered in the current auction so the cashier can choose an available or preferred number. LAN Cashier loads this list through the host API.
- The Add Buyer split panel is initialized at its intended size before applying minimum widths and divider position, preventing startup failure on Cashier workstations.
- Cashier provides prominent Add Buyer and Add Seller actions in its branded workspace. Seller registration lists existing current-auction Buyer Numbers and can reuse the same number by linking the Seller to that existing Buyer identity.
- Cashier supports a visible Full Screen button plus F11/Escape operation, larger uniform action buttons, branded header colors, clearer grouping, and consistent data-grid styling.
- Package the branded Cashier workspace and same-number existing Buyer/Seller linking correction as upgrade version 0.7.14.

## Unpaid buyer review

- Cashier provides an on-demand current-auction list of Buyer Number, name, invoice total, paid amount, and balance due, with an explicit all-paid state.
- Every press reloads host-owned invoice data; LAN Cashier uses the paired API.

## Consignor/Buyer settlement offset

- Consignor save creates or links the same-number current-auction Buyer registration and records a durable `LinkedBuyerId`; conflicting ownership is rejected.
- Settlement detail and printing show gross sales, commission/fees, linked buyer unpaid-purchase deduction, adjusted net, payouts, and remaining balance.
- Operator-facing screens and printed settlement documents use Seller terminology while internal Consignor schema/type names remain unchanged for compatibility.
- A new Seller Number is preflighted against current-auction Buyer Numbers before the seller row is saved. A conflict stops the save with a clear request to choose another number; otherwise the matching Buyer registration is created and linked automatically.
- Renumbering a seller whose linked buyer is already registered under another current-auction number is rejected before saving, and the message identifies the existing Buyer Number instead of exposing a SQLite constraint error.
- The printed deduction label is shortened to `Buyer balance deducted` so it cannot overlap the amount column.

## Credit-card processing fee

- Current Auction configures `CreditCardFeePercent` (default 2.65%) and `CreditCardFeeFixedAmount` (default $0.15). Card payments add and pay both fee components on only their card portion; cash/check portions have zero fee.
- Invoice UI, persistence, and printout show processing fees separately.
- The payment-method dropdown is the only fee selector: selecting Card fills the current invoice balance and immediately previews the 2.65% + $0.15 current-auction fee, while Cash and Check explicitly show no card fee. Pending split-payment rows show fee and total tender before recording.
- Package the corrected card-fee calculation and Card-selection balance autofill as upgrade version 0.7.12.
- On-screen totals and invoice print preview include the currently selected Card fee before recording; persistence remains deferred until Record Payments succeeds.
- Package the pending card-fee invoice preview correction as upgrade version 0.7.13.
- Card entry labels the base invoice amount separately from the calculated card charge, refreshes the full remaining base amount on every Card selection or invoice lookup, subtracts pending split-payment amounts, and records the fee-inclusive total as paid locally or through the LAN cashier.
- Card preview text always displays the current Auction's configured percentage and fixed amount rather than a hard-coded description.
- Package the full-card payment entry and recording correction as upgrade version 0.7.18.
- Looking up a bidder fills the payment amount with the remaining invoice balance for Cash, Check, or Card; changing methods and adding or removing pending split payments refreshes the suggested remainder.
- Package the all-method bidder-balance autofill as upgrade version 0.7.19.

## End-of-night settlement

- Record clerk, auctioneer, floor-help, and kitchen-help payments in an append-only current-auction ledger.
- Corrections require a reason and append linked void/replacement entries; originals remain intact and audited.
- Current-data report and print preview reconcile hammer, premiums, card fees, sales tax, invoice paid/unpaid, consignor commissions/offsets/payouts, staff payments, and net cash movement without counting internal offsets as cash.
- Present the workflow in a branded, full-screen-capable dashboard with clear financial summary cards, organized payment entry, a readable ledger, explicit selection/void guidance, refresh feedback, and uniform actions.
- Package the End-of-Night usability redesign as upgrade version 0.7.20.
- Keep all End-of-Night financial values readable at short/full-screen resolutions by using two taller summary rows and a vertically scrollable fixed-height workspace instead of compressing sections.
- Package the End-of-Night short-screen layout correction as upgrade version 0.7.21.
- Give each End-of-Night section title and subtitle independent DPI-safe vertical space so headings remain fully readable under Windows display scaling.
- Package the End-of-Night scaled-heading correction as upgrade version 0.7.23.

## Invoice condition of sale

- Auction Setup captures optional auction-company name, address, phone, and email information for printed documents through additive schema migration 15.
- Invoice/receipt printing uses the embedded HammerTrack Pro logo, auction-company and bidder contact information, itemized purchases and payment history, structured totals, paid/unpaid status, configured footer, warranty language, and print timestamp.
- End-of-Night printing uses matching branding, auction-company information, grouped buyer revenue and seller/operating expense sections, highlighted net cash movement, staff-payment ledger, and review signature lines.
- Package the professional printed-document redesign as upgrade version 0.7.22.
- Reserve a DPI-safe 340-pixel print-header title region so the complete End-of-Night report heading is never clipped, and suppress an empty Location label.
- Package the End-of-Night print-title correction as upgrade version 0.7.24.
- Every local or LAN Cashier invoice preview/print includes the approved Condition of Sale – Disclaimer of Warranties with its exact heading and two-paragraph wording.

## Bidder cards

- Cashier provides a dedicated Bidder Cards workspace using the current auction's registered bidder list on either the local host or paired LAN cashier.
- The operator selects one bidder and opens a full-letter-page print preview containing that bidder number in very large block numerals, a bold divider, and the persisted auction-company name at the bottom; Auction Name is the fallback when Company Name is blank.
- Bidder-card selection rejects numbers that are not registered in the current auction and never prints registrations from another auction.
- Package bidder-card printing as upgrade version 0.7.25.
- Force bidder-card documents to landscape at both document setup and printer page-query time, and reserve a ten-percent fitting buffer so all selected bidder-number digits remain visible in preview and print.
- Package the landscape bidder-card correction as upgrade version 0.7.26.

## Optional bidder details

- Add Buyer on the local host and paired LAN cashier exposes optional first and last name, address, city, state, ZIP, phone number, email, and driver's license number fields.
- Only the current-auction bidder number is required; every personal/contact field may be left blank.
- The existing Buyers schema already persists these fields, so this user-interface correction requires no database migration and remains compatible with existing databases.
- Package optional cashier bidder details as upgrade version 0.7.27.

## Driver's-license camera scanning

- Explicit owner approval brings forward a focused portion of the later barcode-scanning backlog: PDF417 driver's-license scanning during bidder registration only.
- Cashier Add Buyer and the main Bidders screen provide Scan Driver's License, open the first available Windows camera, support switching among up to five camera indexes, and retain manual entry as the fallback.
- Camera frames are decoded locally as PDF417. The AAMVA parser maps first/last name, street, city, state, ZIP, and driver's-license number into the existing optional bidder fields for review before save.
- Camera frames and the raw barcode payload are never persisted, logged, transmitted to the host, or added to audit text; only the operator-reviewed existing bidder fields are saved through the normal local or paired-cashier workflow.
- The camera window gives actionable no-camera/read errors and releases the camera and preview image when closed.
- Automated tests cover AAMVA parsing, rejection of unrelated PDF417 data, generated PDF417 decoding, empty-frame behavior, and the registration button. Physical camera focus, permission, and real-license variation require acceptance testing on the cashier workstation.
- OpenCvSharp provides Windows camera/frame capture; ZXing.Net's Windows compatibility binding restricts decoding to PDF417. Both dependencies use Apache-2.0 licensing and are packaged locally in the self-contained installer.
- No database migration is required because the scanner fills the existing Buyer fields introduced in migration 8.
- Package driver's-license camera scanning as upgrade version 0.7.28.
- Improve real-camera recognition with 1920x1080 capture, a visible positioning guide, enlarged contrast-equalized and adaptive-threshold decode attempts, relaxed valid-AAMVA header handling, and live actionable recognition status.
- Verify decoding against a low-contrast PDF417 license barcode placed inside a camera-sized frame.
- Package the camera recognition correction as upgrade version 0.7.29.

## Handheld driver's-license scanner

- Cashier Add Buyer and the main Bidders screen provide Use Handheld Scanner alongside Scan with Camera.
- Accept PDF417-capable scanners operating as a USB HID/keyboard through a USB cable, 2.4 GHz receiver, or Bluetooth; no vendor driver, serial-port assignment, or database/network change is required in keyboard mode.
- Capture the scanner keystroke stream in a dedicated private window. Try parsing after the normal 550-millisecond pause; if the data is incomplete, retain it privately for an additional 1.8-second chunk window and retry instead of clearing it. This preserves fast behavior while allowing slower workstations to assemble the complete transmission.
- Never display, log, persist, or transmit the raw barcode. Parse locally and return only the existing bidder fields for operator review before the normal save operation.
- Show actionable PDF417/HID guidance after an unrelated or incomplete scan, clear the rejected buffer, and allow immediate rescan.
- Automated tests cover character-stream collection, AAMVA parsing, successful completion, rejection/reset, and visibility of both camera and handheld actions.
- Package handheld driver's-license scanner support as upgrade version 0.7.30.
- Accept NETUM HID streams that replace AAMVA line separators with Tab or remove them entirely, capture Tab without moving focus, and show live character-receipt feedback before parsing.
- Package the NETUM handheld compatibility correction as upgrade version 0.7.31.
- Capture NETUM keyboard-wedge input through a dedicated private receiver so rapid typed and paste-style HID payloads cannot bypass the scanner window; discard each received block immediately after forwarding it to the scan view model.
- Package the dedicated NETUM keyboard receiver correction as upgrade version 0.7.32.
- Treat optional AAMVA elements such as DCF, DCG, DCL, DCM, DCR, DDA-DDF, and DAW as boundaries in collapsed NETUM streams so they are never appended to the driver's-license number.
- Package the collapsed AAMVA field-boundary correction as upgrade version 0.7.33.
- Reject apparent field markers that overlap by one or two characters with the next real identity marker, preserving names ending in D or DA such as DAVID, RICHARD, and WANDA in collapsed scanner streams.
- Package the collapsed-name boundary correction as upgrade version 0.7.34.
- Package the slower-workstation scanner settling correction as upgrade version 0.7.35.
- Replace the fixed long delay with adaptive incomplete-scan retention and package the cross-workstation correction as upgrade version 0.7.36.

## Live Auction quantity and recorded-sales workflow

- Record a positive quantity on each buyer sale without maintaining or enforcing a lot-level inventory limit.
- Treat Hammer Price as the per-item price; buyer invoices, seller settlements, dashboards, and end-of-night totals use `Quantity * HammerPrice`.
- Display live entry fields in the operator-requested order: Lot Number, Hammer Price, Bidder Number, Quantity.
- Show a scrollable current-lot sales table directly beneath the action buttons with Lot, Bidder, Quantity, Price Each, and Total.
- Creating a new lot description closes the currently selected open lot, assigns the next lot number, and moves directly into the new item workflow.
- Invoice/receipt purchase rows display Item, Quantity, Price, and Total; seller settlement rows show quantity, unit price, and line total.
- Migration 16 adds `Sales.Quantity` with a positive constraint and default of one, retaining all existing sale values and supporting version-3 database upgrades.
- Package the quantity and faster next-description workflow as upgrade version 0.7.37.

## Cashier tender and bidder-card correction

- Fit complete four-, five-, and six-digit bidder numbers on the full-page landscape bidder card.
- Looking up a buyer continues to fill Invoice Amount with the current unpaid balance.
- Cash payments show a separate Cash Tendered entry and calculate Change Due before Add Payment, preventing manual subtraction mistakes.
- Check and Card payments keep Cash Tendered disabled and show zero change.
- Package the correction as upgrade version 0.7.38.

## Audited buyer refunds and sale corrections

- Add Cashier Refund and Correct Sale actions with required confirmation and reason.
- Tie every refund to an original payment and limit it to the remaining purchase portion; card-processing fees are retained.
- Require sufficient refund before reducing, removing, or moving a paid sale.
- Preserve original sale rows as voided history and optionally create a linked replacement for a corrected bidder, quantity, or price.
- Show Refund History on Cashier and include refunds on printed invoices.
- Remove camera-scan buttons. Preserve Scan ID unchanged and add Scan New ID instructions for reflective polycarbonate cards and regular/inverse PDF417 scanner configuration.
- Migrations 17 and 18 add correction metadata and the append-only Refunds ledger.
- Package these approved corrections as upgrade version 0.7.39.

## Seller item summary and NT-1228BL guidance

- Keep the existing Print Settlement option unchanged.
- Add Print Item Summary grouped by lot, description, and price each, with quantity sold and line total.
- Print Total Items Sold and Total Hammer Sales at the end of the new summary.
- Update Scan New ID instructions for the NETUM NT-1228BL PDF417 Regular/Inverse Decode > Decode Both option (called Read Normal Phase / Phase Reversal in the older full manual).
- Package the update as version 0.7.40.

## Saved-auction identification and bidder reuse

- Identify every saved auction by date, auction number, auction name, and status instead of displaying the model type name.
- Let an operator duplicate setup values and optionally register the same durable buyers under their existing bidder numbers in the new auction.
- Start duplicated auctions without lots, sales, payments, refunds, or prior invoice balances; copied registrations receive fresh zero-dollar invoices.
- Keep seller copying as an independent option.
- Package the update as version 0.7.41.

## Cashier payment visibility

- Keep Add Payment, Remove Selected, and Record Payments together in a reserved non-wrapping cashier row after buyer lookup.

## New-ID scanner diagnostics

- Preserve the proven Scan ID behavior for older licenses.
- On Scan New ID, detect when the NETUM scanner delivered no barcode characters and provide an audible, actionable PDF417/Normal Mode/USB HID diagnostic.
- Package the cashier and scanner corrections as version 0.7.42.

## Live-auction keyboard entry speed

- Use Seller Number, Lot Description, Hammer Price, Bidder Number, and Quantity as the rapid keyboard entry sequence; Enter in Lot Description creates the lot and Enter in Quantity records the sale.
- Skip Lot Number and action buttons during the normal Tab sequence.
- Select the complete Hammer Price, Bidder Number, and Quantity values whenever each field receives focus, allowing immediate replacement of 0.00, 0, and 1.
- Increase recorded-current-lot grid header and row heights and padding so cells are not clipped.
- Package the correction as version 0.7.43.

## Clerk search and additional buyers (0.7.60)

- Owner approved searching previously sold items from the clerk screen and adding another bidder at the recorded price.
- Owner confirmed bidding has finished but the auction remains open; formally closed auctions stay locked.
- Provide description/lot search, explicit original price selection, Retail Quantity and Wholesale Deals entry with locked price/deal size.
- Append the purchase and audit linkage, recalculate the receiving invoice, and preserve existing purchase amounts and payments.
- Deliver MVVM presentation, automated business/persistence tests, current manual instructions/screenshots, and a version-matched installer.
- No schema change is required; see [implementation and acceptance checks](SoldItemSearch.md).
