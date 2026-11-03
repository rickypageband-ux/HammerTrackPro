# HammerTrack Online web-platform architecture

## Approved product split

HammerTrackOnline.com will provide two related but operationally separate services:

1. **HammerTrack Online** provides marketplace-style timed auctions, bidder accounts, auction registration, maximum/proxy bids, configurable increments, reserves, soft closing, invoices, and connected credit-card checkout.
2. **HammerTrack Live** provides scheduled camera-enabled auctions. Remote bidders receive bidder numbers, pass a camera/microphone check, and use a Raise Paddle queue. The auctioneer or clerk accepts bids. Payment is arranged by the auction company after the event and is not processed by the Live module.

The existing HammerTrack Pro desktop application remains capable of operating a local auction without internet access. The web platform must not open, copy, or share the desktop SQLite database. Future synchronization will use authenticated, idempotent HTTPS operations through a dedicated bridge/outbox boundary.

## First web-development slice

`HammerTrackLive.Web` is an isolated .NET 8 Razor Pages application suitable for Linux reverse-proxy hosting. It currently contains:

- HammerTrack Online branding and responsive navigation, with HammerTrack Live presented as the camera-enabled live-event mode.
- Public home, auction catalog, auction detail, format-comparison, pricing, and auctioneer pages.
- Clearly marked fictional demonstration auction data.
- An `IAuctionCatalogService` boundary so database persistence can replace demonstration data without changing pages.
- A `/health` endpoint for Plesk or external uptime monitoring.
- Automated catalog and pricing tests.

The deployed controlled-development release now has PostgreSQL-backed invite-only identity, organization membership, durable roles, rate-limited login, lockout, and protected account pages. Public registration remains disabled. It does **not** accept auction registrations, bids, cards, or payments; those actions remain disabled until their authorization, auditing, payment onboarding, webhook validation, and production terms are implemented and tested.

## Target production topology

```text
Plesk / nginx
  -> HammerTrackLive.Web (ASP.NET Core)
      -> PostgreSQL (tenant, auction, bidder, bid, invoice, audit data)
      -> S3-compatible object storage (lot images and documents)
      -> Redis / managed realtime service (ephemeral fan-out only)
      -> Stripe Connect or another approved provider
      -> managed email and optional SMS
      -> managed WebRTC service for HammerTrack Live video
```

PostgreSQL remains the source of truth. Redis/realtime transports may distribute state but never decide or permanently store the winning bid. Live video is isolated from bid ordering.

## Planned ordered migrations

The web database will start in a later feature commit with an explicit migration system. Initial planned entities are Organization, OrganizationUser, Auction, AuctionTermsSnapshot, Lot, LotMedia, Bidder, AuctionRegistration, MaximumBid, BidEvent, Invoice, PaymentReference, ServiceFee, PaymentProviderConnection, WebhookEvent, SyncOutbox, and AuditEvent.

Every bid mutation will require an idempotency key, server-assigned sequence, UTC timestamp, auction scope, bidder registration, and append-only audit record. Commercial terms are snapshotted on the auction and never hard-coded into runtime calculations.

## Payment-provider boundary

The first supported provider is expected to be Stripe Connect, subject to account approval and final commercial/legal review. The platform will:

- Use hosted provider onboarding and hosted/embedded payment components.
- Never store raw card numbers, CVV values, or secret keys in the database or source tree.
- Associate each auction company with its own connected merchant account.
- Verify signed webhooks and store provider event IDs idempotently.
- Record processor references, amounts, refunds, disputes, and platform fees without treating the platform as the card-data system of record.
- Keep the platform fee separate from the auction company's buyer premium, taxes, shipping, and processor fees.

Additional providers must implement the same internal payment interface and receive their own security and accounting review.

## Deployment boundary

The existing Plesk server can host development and a controlled pilot if it supports the .NET 8 runtime, application process supervision, nginx reverse proxying, HTTPS, and resource monitoring. Production photos, documents, database backups, and video must not live only on that server. Production approval requires load tests, restore drills, external object storage, an independently backed-up database, and a defined outage procedure.

## Open decisions

- Final legal entity and merchant-of-record/payment-facilitator structure.
- Stripe Connect account type and fund-flow model.
- Identity-verification level and bidder approval rules.
- Initial concurrent-bidder, lot-volume, image-volume, and event-video targets.
- Bid retraction, auction cancellation, dispute, chargeback, refund, shipping, and prohibited-item policies.
- Whether any live event is recorded, who may access it, and its retention/consent policy.
- Final public pricing after processor, video, storage, email, and support costs are measured.
