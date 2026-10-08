<?php
declare(strict_types=1);
$configFile = __DIR__ . '/config.php';
$config = is_file($configFile) ? require $configFile : [];
$contactEmail = trim((string)($config['contact_email'] ?? 'sales@hammertrackpro.com'));
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="description" content="Try HammerTrack Pro auction management software with a full seven-day Retail demo for your auction operation.">
  <meta name="theme-color" content="#a40000">
  <title>7-Day Demo | HammerTrack Pro</title>
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
      <a class="active" href="demo.php">7-Day Demo</a>
      <a href="index.php#contact">Contact</a>
      <a class="nav-cta" href="index.php?topic=7-Day+Demo#contact">Request demo</a>
    </nav>
  </header>

  <main id="main">
    <section class="demo-hero">
      <div class="demo-hero-copy">
        <p class="eyebrow light">Seven-day HammerTrack Pro demo</p>
        <h1>Run a real auction.<br><span>See the difference.</span></h1>
        <p class="demo-lede">Put HammerTrack Pro through a complete Retail auction workflow—from setup and bidder registration to live sales, checkout, and seller settlement.</p>
        <div class="demo-actions">
          <a class="button button-red" href="index.php?topic=7-Day+Demo#contact">Request your demo</a>
          <a class="button button-ghost" href="#included">See what’s included</a>
        </div>
        <ul class="demo-quick-facts" aria-label="Demo highlights">
          <li><strong>7 full days</strong><span>Begins when your key is issued</span></li>
          <li><strong>Real workflow</strong><span>Use your own auction data</span></li>
          <li><strong>Your work stays</strong><span>Upgrade without starting over</span></li>
        </ul>
      </div>
      <figure class="demo-artwork">
        <img src="assets/hammertrack-pro-package.png" alt="HammerTrack Pro auction software package showing bidder registration, live sales, buyer checkout, and seller settlement">
      </figure>
    </section>

    <section class="demo-band" aria-label="Demo capabilities">
      <p>Retail auction workflow</p><i></i><p>No auction-count limit</p><i></i><p>Works offline</p><i></i><p>Local data ownership</p>
    </section>

    <section id="included" class="demo-included">
      <div class="demo-section-heading">
        <p class="eyebrow">Use it where it matters</p>
        <h2>Seven days to run the room from start to sold.</h2>
        <p>This is a working Retail demo—not a slideshow. Set up an auction, enter real records, print documents, and experience the same focused workflow your team will use on auction day.</p>
      </div>
      <div class="demo-feature-grid">
        <article><span>01</span><h3>Prepare the auction</h3><p>Configure auction terms, register buyers and sellers, and organize lots before the first item sells.</p></article>
        <article><span>02</span><h3>Clerk live sales</h3><p>Record bidders, quantities, hammer prices, and sold items through the fast Retail entry workflow.</p></article>
        <article><span>03</span><h3>Checkout buyers</h3><p>Create invoices, record payments and refunds, and keep every correction auditable.</p></article>
        <article><span>04</span><h3>Settle with sellers</h3><p>Calculate commissions, prepare settlement records, print documents, and back up your auction data.</p></article>
      </div>
    </section>

    <section class="demo-steps">
      <div class="demo-steps-copy">
        <p class="eyebrow light">Simple activation</p>
        <h2>Tell us when you’re ready. We’ll start your seven days.</h2>
        <p>Each demo uses a signed key made for your computer. Because the seven-day clock begins when the key is issued, we create it when you are ready to install and begin.</p>
      </div>
      <ol>
        <li><b>1</b><div><h3>Request the demo</h3><p>Send us your name, email, and a little about your auction operation.</p></div></li>
        <li><b>2</b><div><h3>Send your Machine ID</h3><p>Open HammerTrack Pro, copy the Machine ID shown on the activation screen, and send it to us.</p></div></li>
        <li><b>3</b><div><h3>Import your signed key</h3><p>We’ll email your demo key. Import it and begin your seven-day evaluation.</p></div></li>
      </ol>
    </section>

    <section class="demo-upgrade">
      <div>
        <p class="eyebrow">A safe path to the full system</p>
        <h2>Keep the work you started.</h2>
        <p>Your auctions, registrations, sales, payments, settings, and backups remain in place when you upgrade to a paid license. The demo includes the complete Retail workflow; driver’s-license scanning and the optional Wholesale module are available with the appropriate paid licenses.</p>
      </div>
      <aside>
        <strong>Ready to put it to work?</strong>
        <p>Request your key when you have time to use the full seven-day window.</p>
        <a class="button button-red" href="index.php?topic=7-Day+Demo#contact">Start my 7-day demo</a>
        <a class="demo-email" href="mailto:<?= htmlspecialchars($contactEmail, ENT_QUOTES) ?>"><?= htmlspecialchars($contactEmail) ?></a>
      </aside>
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
