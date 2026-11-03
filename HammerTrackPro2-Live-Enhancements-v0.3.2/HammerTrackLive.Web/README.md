# HammerTrackOnline.com web application

This is the first owner-approved HammerTrack Online web-development slice. It is a .NET 8 Razor Pages application for an Ubuntu/Plesk server. HammerTrack Live is the live-event mode within this platform. The current release is a public development preview only: it has no database, accounts, bids, cards, or payment processing.

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
- External monitoring of `/health`.

Do not place future Stripe secrets, database passwords, SMTP passwords, private license-signing keys, uploaded images, or backups in the deployment archive or public web root.

## Publish

From the repository root:

```powershell
dotnet publish .\HammerTrackLive.Web\HammerTrackLive.Web.csproj -c Release -r linux-x64 --self-contained false -o .\output\hammertracklive\publish
```

Upload the contents of `output/hammertrackonline/publish` to the private application directory selected for HammerTrackOnline.com. Do not upload source, tests, `.git`, or any HammerTrack Pro license issuer files.

Copy `appsettings.Production.example.json` to `appsettings.Production.json` on the server and set `DataProtection:KeysPath` to a persistent service-account-writable location such as `/var/lib/hammertrackonline/keys`. The production configuration file is server-managed and must not be copied back into source if it later contains secrets.

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

## Not production-ready yet

Do not enable public registration or advertise live bidding from this slice. Invite-only authentication and organization tenancy are present, but confirmation email, password recovery, administrator provisioning, append-only bidding, Stripe Connect onboarding and signed webhooks, object storage, notifications, backup/restore, load tests, and outage procedures remain required.
