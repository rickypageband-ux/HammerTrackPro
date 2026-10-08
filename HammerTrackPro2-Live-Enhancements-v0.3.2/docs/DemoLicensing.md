# Seven-day demo licenses (0.7.61)

Owner-approved scope: require an issuer-provided demo key; retain all normal auction features except driver's-license scanning and Wholesale. No bidder, seller, lot, sale, or auction count limits are introduced.

## Issue a demo

1. Give the customer the 0.7.61 MSI. They open HammerTrack and use **Copy Machine ID** on the activation screen. For two workstations collect both IDs.
2. On your private issuer computer, run the following from the source folder. Replace the customer name, IDs and output folder. The private key stays on this computer; never send it or the issuer utility to customers.

```powershell
dotnet run --project .\HammerTrackPro2.LicenseIssuer\HammerTrackPro.LicenseIssuer.csproj -c Release -- issue-demo --key "$env:LOCALAPPDATA\HammerTrackPro\LicenseIssuer\hammertrack-license-private.pem" --customer "Customer Name" --machines "CUSTOMER-PC-ID" --output "$env:USERPROFILE\Desktop\Customer-Demo.htplicense"
```

3. Send only the resulting `.htplicense` file to the customer. They choose **Import License** and select that file. This signed file is their activation key.

The seven days start **when the key is issued**, not when the customer installs/imports it. The issuer prints its exact UTC expiration and the application displays local expiration. Issue the key when the customer is ready. `--expires` is intentionally rejected for demos. Reimporting, reinstalling or reopening the same key does not move its signed deadline. A newly issued key is a deliberate new seven-day grant by the owner.

## What the demo permits

Auction setup; manual buyer and seller registration (including manually entered license numbers); Retail lot entry and clerking; sold-item search/additional buyers; invoices, payments, refunds and corrections; seller settlements; printing; backups; and the existing paired HTTPS cashier workflow. Every workstation needs a valid license covering its own Machine ID. No new network setup or database-sharing path is introduced.

Driver's-license scanner actions are blocked in both main registration and cashier registration. Scanner processing has a second check. Wholesale cannot be opened or activated with a demo, even if a module activation is already installed. Wholesale lot creation/sales are also checked in AuctionService.

## Upgrade to paid

Issue a normal paid license with the existing `issue` command (same customer Machine IDs):

```powershell
dotnet run --project .\HammerTrackPro2.LicenseIssuer\HammerTrackPro.LicenseIssuer.csproj -c Release -- issue --key "$env:LOCALAPPDATA\HammerTrackPro\LicenseIssuer\hammertrack-license-private.pem" --customer "Customer Name" --machines "CUSTOMER-PC-ID" --output "$env:USERPROFILE\Desktop\Customer-Paid.htplicense"
```

While running, the customer clicks **Import / Upgrade License** in the bottom status bar. After expiry they use **Import License** on the activation screen. The paid license replaces the demo and unlocks the scanner immediately. Wholesale still requires its separately sold module activation key. Auction databases, registration, sales, payments, settings and backups are preserved. Invalid, wrong-machine or expired imports leave the installed license unchanged.

## Expiration and compatibility

The application checks the signed license at startup. During operation a one-second UI monitor displays activation when it expires; new local database operations and remote cashier service calls also check the session license. The host's database check protects paired API calls, including while an expired host has the activation dialog open. Already-started database transactions may finish atomically; no partial sale is intentionally interrupted. Canceling activation after expiry exits the application, with existing data retained.

The signed expiry is exclusive: access stops at that instant. The demo rejects clocks earlier than its issue date. A monotonic session timer prevents turning the clock back during a running session from extending it. This is offline licensing, not tamper-proof DRM: an administrator who changes the clock between launches within the signed window, restores the OS, or modifies binaries can circumvent local controls. There is no online time authority or activation server in this release.

The optional signed `Edition` field is omitted for paid licenses, preserving the original five-field signing representation and previously issued paid keys. Demo licenses require 0.7.61 or later; earlier builds do not understand their signed representation. The issuer remains outside the MSI. No production private key is needed for tests.

No database schema change or migration is required (schema remains 20). Rollback: back up the database with the normal workflow before downgrading software; older versions require a paid license because they cannot validate demo keys. Licensing import uses an atomic replacement in the existing Settings folder and never restores or clears auction data.

## Validation

Automated tests cover legacy paid signatures; exact seven-day boundaries; invalid signatures/machines/dates; attempts to remove the demo designation; scanner/module restrictions; rejected imports preserving the old file; paid upgrade; a disposable Retail sale/payment/settlement/backup; preservation after expiry; and paired host access after expiration. Existing database migration and auction tests run as regression checks.

Release verification: 145 tests passed; synthetic-key issuer-to-application validation passed; demo status/upgrade controls visually inspected and covered by a layout regression test. The 0.7.61 installer is unsigned. No production license or signing key was used during validation.
