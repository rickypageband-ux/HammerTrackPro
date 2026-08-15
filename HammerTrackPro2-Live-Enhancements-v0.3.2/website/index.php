<?php
declare(strict_types=1);

$configFile = __DIR__ . '/config.php';
$config = is_file($configFile) ? require $configFile : [];
$checkoutUrl = trim((string)($config['wholesale_checkout_url'] ?? ''));
$contactEmail = trim((string)($config['contact_email'] ?? ''));
$purchaseHref = $checkoutUrl !== '' ? $checkoutUrl : '#contact';
$purchaseLabel = $checkoutUrl !== '' ? 'Buy the upgrade — $299' : 'Request the upgrade — $299';
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="description" content="HammerTrack Pro auction management software for live entry, bidder registration, cashiering, settlements, and wholesale pass-out sales.">
  <meta name="theme-color" content="#102a43">
  <title>HammerTrack Pro | Auction-day software that keeps moving</title>
  <link rel="stylesheet" href="assets/styles.css">
  <script src="assets/site.js" defer></script>
</head>
<body>
  <a class="skip-link" href="#main">Skip to content</a>
  <header class="site-header">
    <a class="brand" href="#top" aria-label="HammerTrack Pro home"><span class="brand-mark" aria-hidden="true">H</span><span>HammerTrack <strong>Pro</strong></span></a>
    <button class="nav-toggle" type="button" aria-expanded="false" aria-controls="site-nav">Menu</button>
    <nav id="site-nav" class="site-nav" aria-label="Main navigation">
      <a href="#features">Features</a><a href="#wholesale">Wholesale</a><a href="#how-it-works">How it works</a><a href="#contact">Support</a>
    </nav>
  </header>

  <main id="main">
    <section id="top" class="hero">
      <div class="hero-copy">
        <p class="eyebrow">Built for the pace of a live auction</p>
        <h1>From bidder card to final settlement—without losing the room.</h1>
        <p class="hero-lede">HammerTrack Pro keeps registration, live sale entry, cashiering, payments, and seller settlements together in one focused Windows system.</p>
        <div class="hero-actions"><a class="button button-primary" href="#contact">Get HammerTrack Pro</a><a class="button button-secondary" href="#wholesale">See Wholesale upgrade</a></div>
        <ul class="trust-list" aria-label="Product highlights"><li>Works offline</li><li>Local auction data</li><li>Two-workstation cashier option</li></ul>
      </div>
      <div class="auction-console" aria-label="Illustration of a live auction sale">
        <div class="console-top"><span>LIVE AUCTION</span><span class="live-dot">Ready</span></div>
        <div class="lot-label">CURRENT LOT</div><div class="lot-number">#184</div><div class="lot-name">Commercial tool assortment</div>
        <div class="console-grid">
          <div><span>Hammer price</span><strong>$1,850</strong></div><div><span>Bidder</span><strong>#427</strong></div>
          <div><span>Quantity</span><strong>12</strong></div><div><span>Status</span><strong class="sold">SOLD</strong></div>
        </div>
      </div>
    </section>

    <section id="features" class="section">
      <div class="section-heading"><p class="eyebrow">One auction-day workspace</p><h2>Clear screens. Fast entry. Auditable records.</h2><p>Built around the jobs your team performs before, during, and after the sale.</p></div>
      <div class="feature-grid">
        <article><span class="feature-number">01</span><h3>Register buyers</h3><p>Keep durable buyer profiles and assign a bidder number for the current auction.</p></article>
        <article><span class="feature-number">02</span><h3>Clerk the sale</h3><p>Keyboard-first lot and buyer entry keeps the operator focused while the auction moves.</p></article>
        <article><span class="feature-number">03</span><h3>Run cashier</h3><p>Itemized invoices, split payments, refunds, corrections, and printable records stay together.</p></article>
        <article><span class="feature-number">04</span><h3>Settle sellers</h3><p>Calculate commissions, record payouts, and print professional settlement summaries.</p></article>
      </div>
    </section>

    <section id="wholesale" class="wholesale-section">
      <div class="wholesale-copy">
        <p class="eyebrow light">Optional licensed module</p><h2>Wholesale Auction upgrade</h2>
        <p>Move fast through quantity-based and pass-out sales while preserving the details your invoices and settlements need.</p>
        <ul class="check-list"><li>Price each × quantity per deal × deals purchased</li><li>Wholesale pass-out entry for repeated buyers</li><li>Locked lot details reduce re-entry during the run</li><li>Works with existing HammerTrack Pro auction records</li><li>Offline, machine-bound activation key</li></ul>
      </div>
      <aside class="price-card" aria-label="Wholesale Auction upgrade price">
        <div class="price-kicker">One-time upgrade</div><div class="price"><span>$</span>299<span class="cents">.00</span></div>
        <p>For licensed HammerTrack Pro customers. Activation is issued for the licensed machine.</p>
        <a class="button button-accent" href="<?= htmlspecialchars($purchaseHref, ENT_QUOTES) ?>"<?= $checkoutUrl !== '' ? ' rel="noopener"' : '' ?>><?= htmlspecialchars($purchaseLabel) ?></a>
        <small>No subscription for this module. Contact us if you need help locating your Machine ID.</small>
      </aside>
    </section>

    <section id="how-it-works" class="section steps-section">
      <div class="section-heading"><p class="eyebrow">Wholesale activation</p><h2>Ready in three steps</h2></div>
      <ol class="steps">
        <li><span>1</span><div><h3>Open Wholesale Auction</h3><p>Choose Wholesale Auction from the HammerTrack Pro dashboard.</p></div></li>
        <li><span>2</span><div><h3>Send your Machine ID</h3><p>Copy the Machine ID shown in the activation window and include it with your order.</p></div></li>
        <li><span>3</span><div><h3>Paste your signed key</h3><p>We issue the activation key; paste it into HammerTrack Pro to unlock the module offline.</p></div></li>
      </ol>
    </section>

    <section id="contact" class="contact-section">
      <div><p class="eyebrow">Sales & support</p><h2>Tell us what you need.</h2><p>Request the Wholesale upgrade, ask about HammerTrack Pro, or get help with an existing installation.</p><?php if ($contactEmail !== ''): ?><p class="direct-email">Or email <a href="mailto:<?= htmlspecialchars($contactEmail, ENT_QUOTES) ?>"><?= htmlspecialchars($contactEmail) ?></a></p><?php endif; ?></div>
      <form class="contact-form" action="contact.php" method="post">
        <input type="text" name="website" tabindex="-1" autocomplete="off" class="honey" aria-hidden="true">
        <label>Name<input name="name" autocomplete="name" required maxlength="100"></label>
        <label>Email<input type="email" name="email" autocomplete="email" required maxlength="200"></label>
        <label>What can we help with?<select name="topic"><option value="Wholesale Auction upgrade">Wholesale Auction upgrade — $299</option><option value="HammerTrack Pro sales">HammerTrack Pro sales</option><option value="Product support">Product support</option></select></label>
        <label>Machine ID <span>(for Wholesale activation)</span><input name="machine_id" maxlength="200" placeholder="Shown in the Wholesale activation window"></label>
        <label>Message<textarea name="message" rows="5" required maxlength="3000"></textarea></label>
        <button class="button button-primary" type="submit">Send request</button><p class="form-note">Your information is used only to respond to this request.</p>
      </form>
    </section>
  </main>
  <footer><a class="brand footer-brand" href="#top"><span class="brand-mark">H</span><span>HammerTrack <strong>Pro</strong></span></a><p>Windows auction management software built for real auction-day work.</p><p>&copy; <?= date('Y') ?> HammerTrack Pro. All rights reserved.</p></footer>
</body>
</html>
