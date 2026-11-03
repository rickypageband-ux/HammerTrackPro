# HammerTrackOnline.com web application

This is the owner-approved HammerTrack Online web platform for an Ubuntu/Plesk server. HammerTrack Live is the live-event mode within this platform. The controlled release uses PostgreSQL-backed invite-only accounts, organization administration, auction/lot catalog management, category and condition choices, lot-specific multi-file media with cover-photo selection, public lot galleries, and publishing. Authorized auction-company administrators can maintain lots and media on Draft and Published auctions; Closed auctions are locked, and published commercial terms remain an immutable snapshot. It does not yet accept auction registrations, bids, cards, or payments.

## Local verification

```powershell
dotnet build .\HammerTrackLive.Web\HammerTrackLive.Web.csproj
dotnet test .\HammerTrackLive.Tests\HammerTrackLive.Tests.csproj
dotnet run --project .\HammerTrackLive.Web\HammerTrackLive.Web.csproj
```

Open the displayed local URL. `/health` must return an HTTP 200 response with `status: ok`.

## Plesk prerequisites

Before installation, confirm the HammerTrackOnline.com subscription has:

- Ubuntu with current security updates.
- Plesk support for an ASP.NET Core application, or permission to run a systemd service behind Plesk nginx.
- The .NET 8 ASP.NET Core Runtime installed.
- A dedicated Linux service account and application directory.
- A writable directory outside the deployment tree for Data Protection keys.
- A valid HammerTrackOnline.com and www.HammerTrackOnline.com SSL certificate.
- PostgreSQL 16 bound only to loopback, with the application connection string supplied as `HAMMERTRACKONLINE_DB` from a root-readable systemd environment file.
- Authenticated SMTP, with account-delivery settings supplied from `/etc/hammertrackonline/account-email.env` and never committed or placed in the web root.
- External monitoring of `/health`.
- A service-account-writable `/var/lib/hammertrackonline/media` directory. This controlled-development store must be included in backups and replaced by external object storage before production approval.

Do not place future Stripe secrets, database passwords, SMTP passwords, private license-signing keys, uploaded images, or backups in the deployment archive or public web root.

## Publish

From the repository root:

```powershell
dotnet publish .\HammerTrackLive.Web\HammerTrackLive.Web.csproj -c Release -r linux-x64 --self-contained false -o .\output\hammertracklive\publish
```

Upload the contents of `output/hammertrackonline/publish` to the private application directory selected for HammerTrackOnline.com. Do not upload source, tests, `.git`, or any HammerTrack Pro license issuer files.

Copy `appsettings.Production.example.json` to `appsettings.Production.json` on the server and set `DataProtection:KeysPath` to a persistent service-account-writable location such as `/var/lib/hammertrackonline/keys`. The production configuration file is server-managed and must not be copied back into source if it later contains secrets.

Copy `deployment/hammertrackonline/account-email.env.example` to `/etc/hammertrackonline/account-email.env`, replace every example value on the server, set ownership to `root:root`, and set mode `0600`. The systemd unit maps those environment values into the `AccountEmail` configuration section. Password-reset and invitation delivery remain unavailable until this file contains working SMTP credentials.

## Process and proxy

Run the published application as a restricted systemd service with:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://127.0.0.1:<private-port>`
- automatic restart on failure
- no public binding of the private application port

Configure the HammerTrackOnline.com Plesk vhost to reverse proxy HTTPS traffic to that loopback port and preserve `Host`, `X-Forwarded-For`, and `X-Forwarded-Proto`. Plesk remains responsible for the public certificate and HTTP-to-HTTPS redirect.

## Required deployment checks

1. `https://hammertrackonline.com/health` returns HTTP 200 and `status: ok`.
2. Home, Auctions, Pricing, How It Works, and For Auctioneers render over HTTPS.
3. An unknown auction slug returns HTTP 404.
4. Browser developer tools show no mixed-content errors.
5. Security response headers include Content-Security-Policy, X-Content-Type-Options, X-Frame-Options, and Referrer-Policy.
6. Restart the service and verify it returns without losing Data Protection keys.
7. Confirm no `.pem`, `.htplicense`, database, payment secret, or SMTP credential exists under the deployment directory.

## Account-management deployment checks

1. Apply ordered idempotent migration `002_AccountInvitations` only after taking a PostgreSQL backup.
2. Create an auction company from `/Platform/Organizations` while signed in as `PlatformAdministrator`.
3. Confirm that the administrator receives a single-use invitation and can create a 12-character strong password.
4. Confirm the accepted account is email-confirmed and appears under the organization.
5. Verify a second administrator can be added before attempting to demote or remove the first; the final administrator safeguard must reject the operation.
6. Request a password reset and verify the same neutral confirmation is displayed for known and unknown email addresses.

For a new installation with no users, run `bootstrap-platform-administrator.sh` once as root after the migration and application deployment. It creates or repairs only the configured first administrator, assigns `PlatformAdministrator`, never prints its generated password, and is idempotent. Immediately use Forgot Password to choose the administrator's private password through the confirmed mailbox.

## Not production-ready yet

Do not enable public registration or advertise live bidding from this slice. Invite-only email confirmation, password recovery, administrator provisioning, and organization management are present. Append-only bidding, Stripe Connect onboarding and signed webhooks, object storage, bidder notifications, production backup/restore, load tests, and outage procedures remain required.
