# HammerTrack Pro 2

HammerTrack Pro 2 is a Windows desktop application for running an auction. It supports bidder registration, consignor management, auction-scoped commercial settings, consignor-owned lots, dashboard totals, database backup, and a keyboard-oriented live-auction screen. Standard lots close after one sale; pass-out mode permits multiple buyer records against one lot until the operator closes it.

## Technology and structure

- .NET 8 Windows Forms (`net8.0-windows`)
- C# with nullable reference types and implicit global usings enabled
- SQLite via `Microsoft.Data.Sqlite` 8.0.13
- `Models/` contains persisted/domain data shapes.
- `Services/` contains validation, auction operations, database location, and SQLite persistence.
- `Forms/` contains the main administration/dashboard UI and live-auction UI.

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

## Data location and backups

The default database is `%ProgramData%\HammerTrackPro2\hammertrack.db`. A workstation can instead use a configured path from `%LocalAppData%\HammerTrackPro2\workstation.json`:

```json
{
  "DatabasePath": "\\\\server\\auction-share\\hammertrack.db"
}
```

The folder is created when possible. In-app backups are written to a `Backups` directory beside the active database. Database files and sidecar files are ignored by source control.

> Assumption/open decision: the configurable path and SQLite shared-cache/WAL settings indicate intended shared or network use, but supported share types, workstation count, locking behavior, permissions, and recovery procedures are not documented or tested in this repository. Validate these before treating a network share as production-safe.

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
- [Contributor and agent guidance](AGENTS.md)

## Current boundaries

There is no CI configuration, packaging/installer, authentication model, or cashier workflow. A dependency-free unit-test harness is in `HammerTrackPro2.Tests`; after restore, run `dotnet run --project .\HammerTrackPro2.Tests\HammerTrackPro2.Tests.csproj`. See the roadmap and current sprint for proposed next steps; proposed items are not commitments until a maintainer approves them.
