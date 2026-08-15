# HammerTrack Pro website — Plesk deployment

This directory is a self-contained PHP website intended for an Ubuntu server managed by Plesk. It does not depend on a third-party application host or database.

## Configure

1. Copy `config.example.php` to `config.php`.
2. Replace `sales@example.com` with the inbox that should receive website requests.
3. Optionally set `wholesale_checkout_url` to a hosted payment link. If it remains blank, the $299 button opens the order-request form instead of pretending that payment was completed.
4. Keep `config.php` out of source control and server backups that are shared publicly.

## Publish with Plesk

1. In Plesk, open the domain's **Hosting & DNS → Hosting** settings and set PHP to a currently supported 8.x release.
2. Upload the contents of this directory into the domain's `httpdocs` folder. Do not upload the containing `website` folder unless it should appear in the URL.
3. Create `config.php` from the example and apply the settings above.
4. In **SSL/TLS Certificates**, issue or renew the free Let's Encrypt certificate and enable permanent HTTPS redirection.
5. Visit `/health.php`; it should return `{"status":"ok","service":"hammertrackpro.com"}`.
6. Submit one test contact request and confirm delivery. Plesk mail may require the domain's SPF/DKIM records and a local `no-reply` mailbox or an SMTP relay.

Before publishing, run `php tests/site_test.php` and PHP's syntax check against each `.php` file. The test confirms the $299 offer, safe order-request fallback, Machine ID field, and activation wording.

## Checkout and fulfillment

The Wholesale module is a one-time $299 upgrade, but its signed machine-bound activation key must still be issued through the private offline license process. Never upload the private signing key or the issuer utility to the web server. If a payment link is configured, its success page should tell the buyer to send the Machine ID displayed in HammerTrack Pro.

## Availability

The site exposes `/health.php` for an external uptime monitor. Configure Plesk Monitoring or an independent monitor to request this URL every five minutes and alert on non-200 responses. This distinguishes a website failure from a browser or DNS problem.
