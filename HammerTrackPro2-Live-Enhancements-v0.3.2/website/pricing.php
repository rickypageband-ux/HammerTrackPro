<?php
declare(strict_types=1);
$configFile = __DIR__ . '/config.php';
$config = is_file($configFile) ? require $configFile : [];
$contactEmail = trim((string)($config['contact_email'] ?? 'Info@rpdavid.com'));
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="description" content="HammerTrack Pro software, complete package, Wholesale upgrade, updates, support, and custom feature pricing.">
  <meta name="theme-color" content="#a40000">
  <title>Pricing | HammerTrack Pro</title>
  <link rel="stylesheet" href="assets/styles.css">
  <script src="assets/site.js" defer></script>
</head>
<body>
  <a class="skip-link" href="#main">Skip to content</a>
  <header class="site-header">
    <a class="brand-logo" href="index.php" aria-label="HammerTrack Pro home"><img src="assets/hammertrack-pro-logo.png" alt="HammerTrack Pro Auction Management Software"></a>
    <button class="nav-toggle" type="button" aria-expanded="false" aria-controls="site-nav">Menu</button>
    <nav id="site-nav" class="site-nav" aria-label="Main navigation">
      <a href="index.php#features">Features</a><a href="index.php#workflow">How it works</a><a class="active" href="pricing.php">Pricing</a><a href="index.php#contact">Contact</a><a class="nav-cta" href="index.php#contact">Request pricing</a>
    </nav>
  </header>

  <main id="main">
    <section class="pricing-hero">
      <p class="eyebrow light">HammerTrack Pro pricing</p>
      <h1>Choose the support your auction business needs.</h1>
      <p>Clear starting prices with direct, personal fulfillment. Submit a request and we’ll confirm the order and next steps by email.</p>
    </section>

    <section class="pricing-page">
      <div class="plan-grid">
        <article class="plan-card">
          <div><p class="plan-label">Core software</p><h2>Auction Software</h2><div class="plan-price"><sup>$</sup>999<span>.00</span></div><p class="price-note">One-time software license</p></div>
          <ul><li>Buyer and seller management</li><li>Live auction entry</li><li>Cashier, invoices, and payments</li><li>Seller settlements and reports</li></ul>
          <a class="button button-dark" href="index.php?topic=Auction+Software#contact">Request Auction Software</a>
        </article>

        <article class="plan-card featured">
          <div class="popular">Complete solution</div>
          <div><p class="plan-label">Whole package</p><h2>Whole Package</h2><div class="plan-price"><sup>$</sup>2,500<span>.00</span></div><p class="price-note">Contact us to confirm the included scope</p></div>
          <ul><li>Built around your auction workflow</li><li>Direct setup coordination</li><li>Personal implementation guidance</li><li>One point of contact</li></ul>
          <a class="button button-red" href="index.php?topic=Whole+Package#contact">Request Whole Package</a>
        </article>

        <article class="plan-card">
          <div><p class="plan-label">Licensed add-on</p><h2>Wholesale Auction</h2><div class="plan-price"><sup>$</sup>299<span>.00</span></div><p class="price-note">One-time machine-bound upgrade</p></div>
          <ul><li>Wholesale quantity and deal tracking</li><li>High-speed pass-out workflow</li><li>Works with existing auction records</li><li>Offline signed activation key</li></ul>
          <a class="button button-dark" href="index.php?topic=Wholesale+Auction+upgrade#contact">Request Wholesale Upgrade</a>
        </article>
      </div>

      <div class="service-grid">
        <article><div><p class="plan-label">Stay current</p><h2>Yearly Updates</h2><p>Keep HammerTrack Pro current with released improvements and updates.</p></div><div class="service-price">$199<span>.00/year</span></div><a href="index.php?topic=Yearly+Updates#contact">Request updates →</a></article>
        <article><div><p class="plan-label">Personal assistance</p><h2>Hourly Support</h2><p>One-on-one help for setup, operation, troubleshooting, or training.</p></div><div class="service-price">$100<span>/hour</span></div><a href="index.php?topic=Hourly+Support#contact">Request support →</a></article>
        <article><div><p class="plan-label">Built for your workflow</p><h2>Upgrades & Feature Requests</h2><p>Tell us what your operation needs. Scope and pricing are confirmed before work begins.</p></div><div class="service-price text">TBD<span>per request</span></div><a href="index.php?topic=Upgrade+or+Feature+Request#contact">Request a quote →</a></article>
      </div>

      <div class="pricing-note"><div><h2>Not sure which option fits?</h2><p>Tell us how you run auctions today and what you want to improve.</p></div><a class="button button-red" href="index.php#contact">Talk with us</a></div>
    </section>
  </main>

  <footer>
    <img src="assets/hammertrack-pro-logo.png" alt="HammerTrack Pro">
    <p>From registration to final invoice—one powerful auction system.</p>
    <nav aria-label="Footer navigation"><a href="index.php">Home</a><a href="pricing.php">Pricing</a><a href="index.php#contact">Contact</a></nav>
    <small>&copy; <?= date('Y') ?> HammerTrack Pro. All rights reserved.</small>
  </footer>
</body>
</html>
