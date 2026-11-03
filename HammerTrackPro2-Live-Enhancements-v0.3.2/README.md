# HammerTrack Pro 2

During pass-out entry, the seller, lot number, description, and hammer price remain locked. The clerk enters only Bidder Number and Quantity; recording a purchase returns focus immediately to Bidder Number, and Close Lot is the only action that advances.

HammerTrack Pro 2 is a Windows desktop application for running auctions. It supports global buyer identities with auction-specific bidder-number registrations, global consignor management with per-auction participation, Auction Setup and selection, auction-owned lots, auction-scoped commercial terms, buyer cashier invoices/payments, closed-auction consignor settlements and payouts, dashboard totals, database backup, and a keyboard-oriented live-auction screen. Each buyer sale records a positive quantity and per-item hammer price; invoices and settlements calculate quantity times price. Standard lots close after one sale; pass-out mode permits multiple buyer records against one lot until the operator closes it.

The dashboard now separates **Retail Auction** from the optional licensed **Wholesale Auction** module. Wholesale sales preserve price each, quantity per deal, deals purchased, total quantity, and sale total. In Wholesale Pass-Out, the lot identity, price, and quantity per deal remain locked while the clerk enters Bidder Number, Deals, and Enter for each buyer. The module button remains visible when locked and accepts a separately signed, machine-bound activation key.

Buyer registration stores optional resale-license number/state and a durable tax-exempt status. Tax-exempt buyers automatically receive zero sales tax at checkout. Seller settlement screens, standard statements, and item-summary prints show the purchasing bidder number for every sold line.

The dashboard includes an offline **Help / User Manual** button. The version-matched customer manual is installed beside the application and remains available without internet access.

The dashboard and Cashier include **Printer Setup**. Each workstation can choose an installed Windows printer once, save that local choice, change it later, and send a test page. HammerTrack print previews and documents use the saved printer; a missing saved printer produces a clear error instead of silently using a different device.

## Technology and structure

- .NET 8 Windows Forms (`net8.0-windows`)
- C# with nullable reference types and implicit global usings enabled
- SQLite via `Microsoft.Data.Sqlite` 8.0.13
- `Models/` contains persisted/domain data shapes.
- `Services/` contains validation, auction operations, database location, and SQLite persistence.
- `ViewModels/` contains functional presentation state/commands for incrementally adopted MVVM modules.
- `Forms/` contains the main administration/dashboard UI, Auction Setup, consignor management, Cashier, and live-auction UI.

The app is intentionally layered as `Forms -> IAuctionService -> IDataStore -> SQLite`. Keep UI code from issuing SQL directly.

## Run locally

1. On Windows, open `HammerTrackPro2.sln` in Visual Studio 2022.
2. Restore NuGet packages if prompted.
3. Build the solution and press F5.

Command-line build:

```powershell
dotnet restore .\HammerTrackPro2.sln
dotnet build .\HammerTrackPro2.sln
```

## Installer and licensing

The repository includes a WiX MSI project for self-contained x64 deployment to multiple PCs. Installed application files live under `%ProgramFiles%\HammerTrack Pro`; persistent auction data, backups, logs, shared workstation settings, and the signed offline license live under `%ProgramData%\HammerTrackPro`. See [Installer and offline licensing](docs/Installer.md) for building the MSI, upgrades, legacy-data migration, and issuing a machine-limited license.

The branded HammerTrack Pro icon is embedded in the application executable, inherited by the installed Desktop and Start Menu shortcuts, and displayed for the product in Windows Apps & Features. The transparent PNG master and multi-resolution Windows icon are maintained in `HammerTrackPro2/Assets`.

For operator setup, print the [two-computer installation guide](output/pdf/HammerTrackPro-Two-Computer-Installation-Guide.pdf). It covers installation, two-PC activation, host/cashier pairing, pre-auction verification, and troubleshooting.

## Two-computer setup

HammerTrack supports one live-auction host PC and one cashier PC on the same private LAN. The host owns the local SQLite database and runs a paired HTTPS cashier service. The cashier never opens or copies the database file.

1. Install/run HammerTrack on the live-auction PC and choose **Live Auction Host** at first start.
2. Allow HammerTrack on **Private networks** if Windows Firewall prompts.
3. Open **Network Information** on the host and keep HammerTrack running.
4. Start HammerTrack on the cashier PC, choose **Cashier Workstation**, and enter the host address, pairing key, and certificate fingerprint shown by the host.
5. The cashier screen shows connection state and retries automatically after a temporary disconnect.

If the computer-name address does not connect, use the alternate IP address displayed by Network Information. Restart HammerTrack after changing a workstation role or connection setting.

## Data location and backups

The host database defaults to `%ProgramData%\HammerTrackPro\hammertrack.db`. Workstation configuration and the offline license are stored in `%ProgramData%\HammerTrackPro\Settings`. A custom host database path must be on a local drive; UNC and detected mapped-network drives are rejected. In-app backups are written to `Backups` beside the host database. A first-run compatibility copy preserves existing data from the former `HammerTrackPro2` locations when the new destination is empty.

## Live-auction keys

- `Enter`: advance through entry fields and record the sale/buyer
- `F2`: repeat the previous buyer in pass-out mode
- `F3`: advance to the queued or next available lot
- `F4`: mark the current unsold item Passed and advance
- `F8`: undo the most recent sale (after confirmation)
- `F11`: toggle fullscreen
- `F12`: close the current lot
- `Esc`: leave fullscreen or close the live-auction window

## Project documentation

- [Project roadmap](docs/ProjectRoadmap.md)
- [Database design](docs/DatabaseDesign.md)
- [Coding standards](docs/CodingStandards.md)
- [Current sprint](docs/CurrentSprint.md)
- [Product backlog](docs/ProductBacklog.md)
- [Contributor and agent guidance](AGENTS.md)

## Website

The self-hosted product website is in `website/`. It is a branded, lightweight PHP site for Ubuntu/Plesk with dedicated home and pricing pages. Published offers include Auction Software ($999), the Whole Package ($2,500), the optional Wholesale Auction module ($299), Yearly Updates ($199), Hourly Support ($100/hour), and custom upgrades quoted per request. All actions currently use a sales-request form; a configured hosted checkout link can be added later. Deployment and uptime-monitoring instructions are in `website/README.md`; the private license-signing key and issuer utility must never be deployed with the website.

The separately approved HammerTrackOnline.com web platform is in `HammerTrackLive.Web/`. It is a .NET 8 Razor Pages application for Ubuntu/Plesk with invite-only confirmed accounts, password recovery, organization-scoped administration, auction-company profiles, timed-auction drafts, lot-specific category/condition/details, multi-photo and PDF uploads, cover-photo selection, commercial-term snapshots, validation, publishing, and a PostgreSQL-backed public catalog with per-lot galleries. Organization administrators may maintain lot details and media after publication so catalog corrections appear publicly; closed auctions remain locked, and the published commercial-terms snapshot remains immutable. Public registration remains disabled, and the site does not yet accept auction registrations, bids, cards, or payments. HammerTrack Live remains the camera-enabled live-event mode within HammerTrack Online. The target production architecture, payment boundary, deployment requirements, and ordered backlog are documented in [HammerTrack Online web-platform architecture](docs/HammerTrackLiveArchitecture.md).

## Current boundaries

There is no CI configuration, user/role authentication model, or auction reopening. A WiX MSI and offline signed machine-license system are included; production code signing remains required before broad distribution. Cashier supports current-auction bidder lookup, itemized invoices, partial/split cash/check/card payments, cash change, status updates, and Windows invoice printing. After an auction is closed, Cashier can look up a current-auction consignor, calculate commission using override → consignor default → auction default, record check/cash/ACH payouts, and print a settlement statement. Closing an auction is audited and locks buyer payments while enabling consignor payouts. Standard xUnit tests are in `HammerTrackPro2.Tests`; after restore, run `dotnet test .\HammerTrackPro2.sln`.

Selecting Card in Cashier fills the current invoice balance and immediately previews the current-auction processing fee, defaulted to 2.65% plus $0.15.
The cashier enters and records the invoice balance before the card fee; HammerTrack then adds the current auction's configured percentage and fixed fee, charges and applies the combined amount, and prints the fee-inclusive paid invoice total.
Looking up a bidder automatically fills the payment amount with the invoice balance owed for Cash, Check, or Card. Pending split payments reduce the automatically suggested remainder.
The cashier payment actions occupy a reserved non-wrapping row, keeping Add Payment, Remove Selected, and Record Payments visible after buyer lookup and in full-screen mode.
Cashier includes a Bidder Cards workspace that lists current-auction registrations and previews a full-letter-page card with the selected bidder number in large block type and the saved auction-company name at the bottom.
Bidder cards fit four-, five-, and six-digit bidder numbers completely on the landscape page. Cashier buyer lookup fills the invoice balance, and Cash payments provide separate Cash Tendered and calculated Change Due fields before the payment is added.
Cashier provides audited Refund and Correct Sale actions. Refunds are tied to an original payment, require a reason, refund purchase funds only, and retain card-processing fees. Paid sales must be refunded before they can be reduced, removed, or moved to another registered bidder; original sales remain stored as voided history.
Seller printing keeps the original settlement statement and adds Print Item Summary. The summary groups matching sales by lot, description, and price each, shows the quantity and line total, and finishes with Total Items Sold and Total Hammer Sales.
Add Buyer offers optional first and last name, full address, phone number, email, and driver's license number fields. Only the current-auction bidder number is required.
Cashier Add Buyer and the main Bidders screen include a Scan Driver's License button. HammerTrack opens a local camera preview, decodes the PDF417 barcode, fills supported AAMVA name/address/license fields for operator review, and does not save the camera image or raw barcode. Manual entry remains available when no compatible camera or readable barcode is available.
The scanner requests a high-resolution frame, displays a dashed positioning guide, and automatically retries enlarged, contrast-enhanced, and black-and-white versions of the live image. Its status line distinguishes a barcode that has not yet been read from a barcode whose license data was not recognized.
Bidder entry provides Scan ID and Scan New ID for PDF417-capable scanners such as the NETUM NT-1228BL; the camera button is no longer shown. Scan ID preserves the proven legacy-card receiver. Scan New ID uses the same private parser and directs NT-1228BL operators to PDF417 > Regular/Inverse Decode > Decode Both (called Read Normal Phase / Phase Reversal in the older full manual). In USB HID/keyboard mode, the USB cable, 2.4 GHz receiver, or Bluetooth connection can supply the license barcode; HammerTrack detects the completed transmission, hides and discards the raw data, and fills the same reviewable bidder fields.
Scan New ID waits without displaying an error until the operator actually scans; it no longer treats five seconds of inactivity as a failed scan.
NETUM streams are accepted when the scanner sends AAMVA fields with line breaks, tabs, or with the separators removed. A dedicated private keyboard receiver accepts both rapid typed and paste-style scanner transmissions, displays a live received-character count, and parses automatically after the scan pauses.
The handheld receiver first parses after the normal short pause. If a transmission is incomplete, it privately retains that partial data for an additional chunk window and retries, supporting both fast and slower workstations without exposing the raw barcode.
Optional AAMVA fields in collapsed NETUM transmissions are treated as boundaries, preventing document-control, restriction, and physical-description data from being appended to the driver's-license number.
Collapsed-stream boundary detection preserves the final letters of names when they overlap visually with the next three-letter AAMVA marker, including names ending in D or DA.

Cashier uses a branded, full-screen-capable workspace with larger uniform controls and direct Buyer and Seller registration. Registering a Seller with an existing same-number current-auction Buyer links the Seller to that durable Buyer identity instead of creating a duplicate or rejecting the registration.

Seller Management uses the same branded workspace, full-screen controls, large uniform actions, searchable seller count, focused directory columns, and clear add/edit/save feedback. Sensitive Tax ID values remain in the details editor rather than the directory grid.

Auction Setup uses the branded full-screen workspace with a clear auction directory, large uniform New/Load/Duplicate/Close/Save actions, current-auction feedback, and an organized details editor. Live Auction keeps all five primary action buttons visible in an equal-width full-card row at normal and full-screen sizes.

Saved auctions are identified in the Auction Setup list by date, auction number, auction name, and status. Duplicating an auction can optionally copy its registered bidders with the same bidder numbers while always starting with blank lots, sales, invoices, and payments.

The operator interface uses **Seller** wording while retaining the existing `Consignor` domain and database names for backward compatibility. Creating a seller also creates and links the same current-auction Buyer Number; if that Buyer Number is already assigned to someone else, saving is stopped so the operator can choose another Seller Number.

Lot entry lists sellers by seller number and name, refreshes after Seller Management closes, and retains inactive sellers for historical/controlled assignment with a visible inactive label.

Live Auction also provides a focused quick-add row for an active Seller Number and lot description. It closes the currently selected open lot, assigns the next current-auction lot number, and makes the new description immediately available for sale. Entry fields appear as Lot Number, Hammer Price (each), Bidder Number, and Quantity. Retail and Wholesale screens reserve the right side for a continuously scrollable, mode-specific history of every current-auction sale, including time, lot, buyer number and name, quantity, price, and total.
Live Auction supports the rapid keyboard path Seller Number, Tab, Lot Description, Enter/Create Lot, Hammer Price, Tab, Bidder Number, Tab, Quantity, Enter. Hammer Price, Bidder Number, and Quantity select their complete existing values on focus so typing replaces 0.00, 0, or 1 immediately. Selecting or double-clicking a history row opens an audited correction workflow for bidder, quantity, deals where applicable, and price. Paid-sale safeguards remain enforced and the original transaction remains as voided history.

End-of-Night uses the same branded full-screen workspace, with clear financial summary cards, organized staff-payment entry, a readable audit ledger, safer void guidance, refresh status, and print preview.

Auction Setup stores optional auction-company name, address, phone, and email details. Buyer invoices/receipts and End-of-Night reports use those details with the embedded HammerTrack Pro logo, professional section styling, bidder contact information, itemized purchases/payments, financial reconciliation, and audit-ledger presentation.

## Search sold items from the clerk screen

Version 0.7.60 adds **SEARCH SOLD ITEMS** below Retail/Wholesale live history. Search a description or lot number, select the recorded price, enter a registered bidder and Quantity (Retail) or Deals (Wholesale), and choose **Add Buyer at This Price**. This records an additional purchase at the stored price, preserving earlier purchases and payments. The auction must remain open. See [Clerk search and additional buyers](docs/SoldItemSearch.md).

## Seven-day demo licenses (0.7.61)

Issuer-provided seven-day demo licenses run complete Retail auctions without bidder/lot limits. Driver's-license scanning and Wholesale are disabled. A paid license upgrades the same installation and preserves auction data; Wholesale still needs its module key. See [Demo issuing and upgrade instructions](docs/DemoLicensing.md).
