# HammerTrack Pro website — Plesk deployment

This directory is a self-contained PHP website intended for an Ubuntu server managed by Plesk. It does not depend on a third-party application host or database.

## Configure

The included `config.php` sends website requests to `Info@rpdavid.com`. The
`wholesale_checkout_url` setting is intentionally blank, so the $299 button
opens the order-request form. A Square payment link can be placed in that
setting later without rebuilding the website.

## Publish with Plesk

1. In Plesk, open the domain's **Hosting & DNS → Hosting** settings and set PHP to a currently supported 8.x release.
2. Upload the contents of this directory into the domain's `httpdocs` folder. Do not upload the containing `website` folder unless it should appear in the URL.
3. In **SSL/TLS Certificates**, issue or renew the free Let's Encrypt certificate and enable permanent HTTPS redirection.
4. Visit `/health.php`; it should return `{"status":"ok","service":"hammertrackpro.com"}`.
5. Submit one test contact request and confirm delivery at `Info@rpdavid.com`. Plesk mail may require the domain's SPF/DKIM records and a local `no-reply` mailbox or an SMTP relay.

Before publishing, run `php tests/site_test.php` and PHP's syntax check against each `.php` file. The test confirms the $299 offer, safe order-request fallback, Machine ID field, and activation wording.

## Checkout and fulfillment

The Wholesale module is a one-time $299 upgrade, but its signed machine-bound activation key must still be issued through the private offline license process. Never upload the private signing key or the issuer utility to the web server. If a payment link is configured, its success page should tell the buyer to send the Machine ID displayed in HammerTrack Pro.

## Availability

The site exposes `/health.php` for an external uptime monitor. Configure Plesk Monitoring or an independent monitor to request this URL every five minutes and alert on non-200 responses. This distinguishes a website failure from a browser or DNS problem.
