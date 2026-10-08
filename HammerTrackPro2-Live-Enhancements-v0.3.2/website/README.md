# HammerTrack Pro website — Plesk deployment

This directory is a self-contained PHP website intended for an Ubuntu server managed by Plesk. It does not depend on a third-party application host or database.

The site uses the supplied HammerTrack Pro logo throughout and includes a
dedicated pricing page for Auction Software, the Whole Package, the Wholesale
Auction upgrade, Yearly Updates, Hourly Support, and quoted feature requests.
The Whole Package lists two laptops, one laser printer, one NETUM scanner, one
network router, on-site setup in South Carolina, and one hour of training.
Service, setup, and training outside South Carolina may require additional fees.
The dedicated `demo.php` page presents the signed seven-day Retail demo, its
included workflow, activation steps, scanner/Wholesale restrictions, and the
data-preserving path to a paid license. Demo requests use the same protected
contact form and delivery address as sales requests.

## Configure

The included `config.php` sends website requests to `sales@hammertrackpro.com`. The
`wholesale_checkout_url` setting is intentionally blank, so the $299 button
opens the order-request form. A Square payment link can be placed in that
setting later without rebuilding the website.

### Enable the contact form with IONOS

The upload package already contains the IONOS SMTP configuration and mail
sender. After uploading the complete website:

1. In Plesk, open **Files → httpdocs → _private**.
2. Open `hammertrack-mail.php` in the editor.
3. Replace only `ENTER_YOUR_IONOS_EMAIL_PASSWORD_HERE` with the mailbox
   password for `Info@rpdavid.com`.
4. Save the file and submit one website request.
5. Confirm the message arrives at `sales@hammertrackpro.com`. The customer's submitted
   address is set as Reply-To.

The included `_private/.htaccess` denies browser access to the SMTP settings.
Do not email, publish, or commit the completed password. If Plesk offers file
permissions, keep the configuration readable only by the subscription's system
user.

### Contact-form spam protection

Every sales, pricing, and upgrade request uses the same protected contact
handler. It rejects hidden-field bot submissions, requires a one-time browser
session token, rejects forms completed in less than two seconds, limits each
visitor to five accepted attempts per hour, and silently discards messages with
more than two web links. No CAPTCHA account or paid anti-spam service is needed.
The sixth submission from the same browser or network displays a clear request
to wait one hour, making the limit easy to verify during testing.

The rate limiter creates `_private/contact-rate-limits.json` automatically. The
Plesk PHP user must be able to write to `_private`; the directory's included
`.htaccess` continues to prevent browser access. The file stores only a SHA-256
hash of the visitor address and recent timestamps, not the original address.

## Publish with Plesk

1. In Plesk, open the domain's **Hosting & DNS → Hosting** settings and set PHP to a currently supported 8.x release.
2. Upload the contents of this directory into the domain's `httpdocs` folder. Do not upload the containing `website` folder unless it should appear in the URL.
3. In **SSL/TLS Certificates**, issue or renew the free Let's Encrypt certificate and enable permanent HTTPS redirection.
4. Visit `/health.php`; it should return `{"status":"ok","service":"hammertrackpro.com"}`.
5. Complete the IONOS password step above, submit one test contact request, and confirm delivery at `sales@hammertrackpro.com`.

Before publishing, run `php tests/site_test.php` and PHP's syntax check against each `.php` file. The test confirms the brand assets, demo disclosures, every published price, the request workflow, the Machine ID field, spam controls, rate limiting, and safe handling of an incomplete SMTP configuration.

## Checkout and fulfillment

The Wholesale module is a one-time $299 upgrade, but its signed machine-bound activation key must still be issued through the private offline license process. Never upload the private signing key or the issuer utility to the web server. If a payment link is configured, its success page should tell the buyer to send the Machine ID displayed in HammerTrack Pro.

## Availability

The site exposes `/health.php` for an external uptime monitor. Configure Plesk Monitoring or an independent monitor to request this URL every five minutes and alert on non-200 responses. This distinguishes a website failure from a browser or DNS problem.
