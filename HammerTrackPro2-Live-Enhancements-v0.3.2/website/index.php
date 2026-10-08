<?php
declare(strict_types=1);
require_once __DIR__ . '/lib/ContactSpamGuard.php';
ContactSpamGuard::startSession();
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');
$contactToken = ContactSpamGuard::issueToken($_SESSION, time());
$configFile = __DIR__ . '/config.php';
$config = is_file($configFile) ? require $configFile : [];
$contactEmail = trim((string)($config['contact_email'] ?? 'sales@hammertrackpro.com'));
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="description" content="HammerTrack Pro auction management software—from bidder registration through final invoice and seller settlement.">
  <meta name="theme-color" content="#a40000">
  <title>HammerTrack Pro | One Powerful Auction System</title>
  <link rel="stylesheet" href="assets/styles.css?v=20261008">
  <script src="assets/site.js" defer></script>
</head>
<body>
  <a class="skip-link" href="#main">Skip to content</a>
  <header class="site-header">
    <a class="brand-logo" href="index.php" aria-label="HammerTrack Pro home"><img src="assets/hammertrack-pro-logo.png" alt="HammerTrack Pro Auction Management Software"></a>
    <button class="nav-toggle" type="button" aria-expanded="false" aria-controls="site-nav">Menu</button>
    <nav id="site-nav" class="site-nav" aria-label="Main navigation">
      <a href="index.php#features">Features</a>
      <a href="index.php#workflow">How it works</a>
      <a href="pricing.php">Pricing</a>
      <a href="demo.php">7-Day Demo</a>
      <a href="index.php#contact">Contact</a>
      <a class="nav-cta" href="demo.php">Try it free</a>
    </nav>
  </header>

  <main id="main">
    <section class="hero">
      <div class="hero-copy">
        <p class="eyebrow">Auction management software</p>
        <h1>Run the room.<br><span>Track every dollar.</span></h1>
        <p class="hero-lede">From registration to final invoice, HammerTrack Pro gives auction teams one focused system for live sale entry, cashiering, payments, and seller settlements.</p>
        <div class="hero-actions">
          <a class="button button-red" href="pricing.php">Explore pricing</a>
          <a class="button button-outline" href="demo.php">Try the 7-day demo</a>
        </div>
        <div class="proof-strip"><span>Works offline</span><span>Local data ownership</span><span>Built for Windows</span></div>
      </div>
      <div class="hero-stage">
        <img class="hero-logo" src="assets/hammertrack-pro-logo.png" alt="HammerTrack Pro">
        <div class="live-card">
          <div class="live-card-top"><span>LIVE AUCTION</span><span class="ready"><i></i> READY</span></div>
          <div class="live-lot"><div><small>LOT</small><strong>184</strong></div><p>Commercial tool assortment</p></div>
          <div class="live-stats"><div><small>HAMMER</small><b>$1,850</b></div><div><small>BIDDER</small><b>#427</b></div><div><small>STATUS</small><b class="sold">SOLD</b></div></div>
        </div>
      </div>
    </section>

    <section class="brand-band" aria-label="HammerTrack Pro promise">
      <p>Registration</p><i></i><p>Live auction</p><i></i><p>Cashier</p><i></i><p>Final settlement</p>
    </section>

    <section id="features" class="section">
      <div class="section-heading">
        <p class="eyebrow">Everything your auction needs</p>
        <h2>One system from first bidder to final payout.</h2>
        <p>Purpose-built tools keep your staff moving quickly while preserving accurate, auditable records.</p>
      </div>
      <div class="feature-grid">
        <article><span>01</span><h3>Buyer registration</h3><p>Maintain buyer profiles and auction-specific bidder numbers without duplicating people between events.</p></article>
        <article><span>02</span><h3>Live sale entry</h3><p>Keyboard-first workflows help your clerk record lots, quantities, hammer prices, and buyers at auction speed.</p></article>
        <article><span>03</span><h3>Cashier & invoices</h3><p>Create itemized invoices, record split payments, issue refunds, and keep correction history auditable.</p></article>
        <article><span>04</span><h3>Seller settlements</h3><p>Calculate commissions, track payouts, and produce professional settlement records after the sale.</p></article>
      </div>
    </section>

    <section id="workflow" class="workflow-section">
      <div class="workflow-copy">
        <p class="eyebrow light">Made for auction day</p>
        <h2>Fast when the room is moving. Thorough when the books close.</h2>
        <p>HammerTrack Pro keeps operational data on the host computer and supports a paired cashier workstation across your private network.</p>
        <a class="text-link" href="pricing.php">Find the right package <span>→</span></a>
      </div>
      <ol class="workflow-list">
        <li><b>01</b><div><h3>Prepare</h3><p>Set auction terms, register buyers and sellers, and organize lots.</p></div></li>
        <li><b>02</b><div><h3>Sell</h3><p>Record retail or licensed wholesale sales through a focused live-entry screen.</p></div></li>
        <li><b>03</b><div><h3>Settle</h3><p>Collect buyer payments, print invoices, and complete seller payouts.</p></div></li>
      </ol>
    </section>

    <section class="pricing-teaser">
      <div><p class="eyebrow light">Straightforward pricing</p><h2>Professional auction software starting at <span>$999</span>.</h2></div>
      <a class="button button-white" href="pricing.php">See all pricing</a>
    </section>

    <section id="contact" class="contact-section">
      <div class="contact-intro">
        <p class="eyebrow">Sales & support</p>
        <h2>Let’s talk about your auction operation.</h2>
        <p>Request software, upgrades, training, or support. We’ll reply directly with the right next step.</p>
        <a href="mailto:<?= htmlspecialchars($contactEmail, ENT_QUOTES) ?>"><?= htmlspecialchars($contactEmail) ?></a>
      </div>
      <form class="contact-form" action="contact.php" method="post">
        <input type="text" name="website" tabindex="-1" autocomplete="off" class="honey" aria-hidden="true">
        <input type="hidden" name="contact_token" value="<?= htmlspecialchars($contactToken, ENT_QUOTES) ?>">
        <label>Name<input name="name" autocomplete="name" required maxlength="100"></label>
        <label>Email<input type="email" name="email" autocomplete="email" required maxlength="200"></label>
        <label>What can we help with?
          <select name="topic" id="topic">
            <option value="7-Day Demo">7-Day Demo</option>
            <option value="Auction Software">Auction Software — $999</option>
            <option value="Whole Package">Whole Package — $2,500</option>
            <option value="Wholesale Auction upgrade">Wholesale Auction upgrade — $299</option>
            <option value="Yearly Updates">Yearly Updates — $199</option>
            <option value="Hourly Support">Hourly Support — $100/hour</option>
            <option value="Upgrade or Feature Request">Upgrade or Feature Request — quoted per request</option>
          </select>
        </label>
        <label>Machine ID <span>(for Demo or Wholesale activation)</span><input name="machine_id" maxlength="200" placeholder="Shown on the HammerTrack Pro activation screen"></label>
        <label>Tell us what you need<textarea name="message" rows="5" required maxlength="3000"></textarea></label>
        <button class="button button-red" type="submit">Send request</button>
        <p class="form-note">Your information is used only to respond to this request.</p>
      </form>
    </section>
  </main>

  <footer>
    <img src="assets/hammertrack-pro-logo.png" alt="HammerTrack Pro">
    <p>From registration to final invoice—one powerful auction system.</p>
    <nav aria-label="Footer navigation"><a href="index.php">Home</a><a href="demo.php">7-Day Demo</a><a href="pricing.php">Pricing</a><a href="index.php#contact">Contact</a></nav>
    <small>&copy; <?= date('Y') ?> HammerTrack Pro. All rights reserved.</small>
  </footer>
</body>
</html>
