# HammerTrack Pro 2

HammerTrack Pro 2 is a Windows desktop application for running auctions. It supports global buyer identities with auction-specific bidder-number registrations, global consignor management with per-auction participation, Auction Setup and selection, auction-owned lots, auction-scoped commercial terms, buyer cashier invoices/payments, closed-auction consignor settlements and payouts, dashboard totals, database backup, and a keyboard-oriented live-auction screen. Standard lots close after one sale; pass-out mode permits multiple buyer records against one lot until the operator closes it.

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

## Two-computer setup

HammerTrack supports one live-auction host PC and one cashier PC on the same private LAN. The host owns the local SQLite database and runs a paired HTTPS cashier service. The cashier never opens or copies the database file.

1. Install/run HammerTrack on the live-auction PC and choose **Live Auction Host** at first start.
2. Allow HammerTrack on **Private networks** if Windows Firewall prompts.
3. Open **Network Information** on the host and keep HammerTrack running.
4. Start HammerTrack on the cashier PC, choose **Cashier Workstation**, and enter the host address, pairing key, and certificate fingerprint shown by the host.
5. The cashier screen shows connection state and retries automatically after a temporary disconnect.

If the computer-name address does not connect, use the alternate IP address displayed by Network Information. Restart HammerTrack after changing a workstation role or connection setting.

## Data location and backups

The host database defaults to `%ProgramData%\HammerTrackPro2\hammertrack.db`. Workstation configuration is stored in `%LocalAppData%\HammerTrackPro2\workstation.json`. A custom host database path must be on a local drive; UNC and detected mapped-network drives are rejected. In-app backups are written to `Backups` beside the host database.

## Live-auction keys

- `Enter`: advance through entry fields and record the sale/buyer
- `F2`: repeat the previous buyer in pass-out mode
- `F3`: advance to the queued or next available lot
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

There is no CI configuration, packaging/installer, authentication model, or auction reopening. Cashier supports current-auction bidder lookup, itemized invoices, partial/split cash/check/card payments, cash change, status updates, and Windows invoice printing. After an auction is closed, Cashier can look up a current-auction consignor, calculate commission using override → consignor default → auction default, record check/cash/ACH payouts, and print a settlement statement. Closing an auction is audited and locks buyer payments while enabling consignor payouts. Standard xUnit tests are in `HammerTrackPro2.Tests`; after restore, run `dotnet test .\HammerTrackPro2.sln`.

Lot entry lists consignors by consignor number and name, refreshes after Consignor Management closes, and retains inactive consignors for historical/controlled assignment with a visible inactive label.
