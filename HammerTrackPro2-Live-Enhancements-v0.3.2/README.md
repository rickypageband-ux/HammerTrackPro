# HammerTrack Pro 2

HammerTrack Pro 2 is a Windows desktop application for running auctions. It supports global buyer identities with auction-specific bidder-number registrations, global consignor management with per-auction participation, Auction Setup and selection, auction-owned lots, auction-scoped commercial terms, buyer cashier invoices/payments, closed-auction consignor settlements and payouts, dashboard totals, database backup, and a keyboard-oriented live-auction screen. Each buyer sale records a positive quantity and per-item hammer price; invoices and settlements calculate quantity times price. Standard lots close after one sale; pass-out mode permits multiple buyer records against one lot until the operator closes it.

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

## Current boundaries

There is no CI configuration, user/role authentication model, or auction reopening. A WiX MSI and offline signed machine-license system are included; production code signing remains required before broad distribution. Cashier supports current-auction bidder lookup, itemized invoices, partial/split cash/check/card payments, cash change, status updates, and Windows invoice printing. After an auction is closed, Cashier can look up a current-auction consignor, calculate commission using override → consignor default → auction default, record check/cash/ACH payouts, and print a settlement statement. Closing an auction is audited and locks buyer payments while enabling consignor payouts. Standard xUnit tests are in `HammerTrackPro2.Tests`; after restore, run `dotnet test .\HammerTrackPro2.sln`.

Selecting Card in Cashier fills the current invoice balance and immediately previews the current-auction processing fee, defaulted to 2.65% plus $0.15.
The cashier enters and records the invoice balance before the card fee; HammerTrack then adds the current auction's configured percentage and fixed fee, charges and applies the combined amount, and prints the fee-inclusive paid invoice total.
Looking up a bidder automatically fills the payment amount with the invoice balance owed for Cash, Check, or Card. Pending split payments reduce the automatically suggested remainder.
Cashier includes a Bidder Cards workspace that lists current-auction registrations and previews a full-letter-page card with the selected bidder number in large block type and the saved auction-company name at the bottom.
Bidder cards fit four-, five-, and six-digit bidder numbers completely on the landscape page. Cashier buyer lookup fills the invoice balance, and Cash payments provide separate Cash Tendered and calculated Change Due fields before the payment is added.
Add Buyer offers optional first and last name, full address, phone number, email, and driver's license number fields. Only the current-auction bidder number is required.
Cashier Add Buyer and the main Bidders screen include a Scan Driver's License button. HammerTrack opens a local camera preview, decodes the PDF417 barcode, fills supported AAMVA name/address/license fields for operator review, and does not save the camera image or raw barcode. Manual entry remains available when no compatible camera or readable barcode is available.
The scanner requests a high-resolution frame, displays a dashed positioning guide, and automatically retries enlarged, contrast-enhanced, and black-and-white versions of the live image. Its status line distinguishes a barcode that has not yet been read from a barcode whose license data was not recognized.
Bidder entry also provides Use Handheld Scanner for PDF417-capable scanners such as compatible NETUM 2D models. In USB HID/keyboard mode, the USB cable, 2.4 GHz receiver, or Bluetooth connection can supply the license barcode; HammerTrack detects the completed transmission, hides and discards the raw data, and fills the same reviewable bidder fields.
NETUM streams are accepted when the scanner sends AAMVA fields with line breaks, tabs, or with the separators removed. A dedicated private keyboard receiver accepts both rapid typed and paste-style scanner transmissions, displays a live received-character count, and parses automatically after the scan pauses.
The handheld receiver first parses after the normal short pause. If a transmission is incomplete, it privately retains that partial data for an additional chunk window and retries, supporting both fast and slower workstations without exposing the raw barcode.
Optional AAMVA fields in collapsed NETUM transmissions are treated as boundaries, preventing document-control, restriction, and physical-description data from being appended to the driver's-license number.
Collapsed-stream boundary detection preserves the final letters of names when they overlap visually with the next three-letter AAMVA marker, including names ending in D or DA.

Cashier uses a branded, full-screen-capable workspace with larger uniform controls and direct Buyer and Seller registration. Registering a Seller with an existing same-number current-auction Buyer links the Seller to that durable Buyer identity instead of creating a duplicate or rejecting the registration.

Seller Management uses the same branded workspace, full-screen controls, large uniform actions, searchable seller count, focused directory columns, and clear add/edit/save feedback. Sensitive Tax ID values remain in the details editor rather than the directory grid.

Auction Setup uses the branded full-screen workspace with a clear auction directory, large uniform New/Load/Duplicate/Close/Save actions, current-auction feedback, and an organized details editor. Live Auction keeps all five primary action buttons visible in an equal-width full-card row at normal and full-screen sizes.

The operator interface uses **Seller** wording while retaining the existing `Consignor` domain and database names for backward compatibility. Creating a seller also creates and links the same current-auction Buyer Number; if that Buyer Number is already assigned to someone else, saving is stopped so the operator can choose another Seller Number.

Lot entry lists sellers by seller number and name, refreshes after Seller Management closes, and retains inactive sellers for historical/controlled assignment with a visible inactive label.

Live Auction also provides a focused quick-add row for an active Seller Number and lot description. It closes the currently selected open lot, assigns the next current-auction lot number, and makes the new description immediately available for sale. Entry fields appear as Lot Number, Hammer Price (each), Bidder Number, and Quantity. A scrollable table directly below the action buttons shows every recorded buyer sale for the current lot with quantity, unit price, and total.

End-of-Night uses the same branded full-screen workspace, with clear financial summary cards, organized staff-payment entry, a readable audit ledger, safer void guidance, refresh status, and print preview.

Auction Setup stores optional auction-company name, address, phone, and email details. Buyer invoices/receipts and End-of-Night reports use those details with the embedded HammerTrack Pro logo, professional section styling, bidder contact information, itemized purchases/payments, financial reconciliation, and audit-ledger presentation.
