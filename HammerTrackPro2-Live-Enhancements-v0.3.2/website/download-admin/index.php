<?php
declare(strict_types=1);

$authenticatedUser = trim((string)($_SERVER['REMOTE_USER'] ?? $_SERVER['PHP_AUTH_USER'] ?? ''));
if ($authenticatedUser === '') {
    http_response_code(403);
    header('Content-Type: text/plain; charset=utf-8');
    echo "This page is disabled until /download-admin is password-protected in Plesk.\n";
    exit;
}

require_once dirname(__DIR__) . '/lib/ContactSpamGuard.php';
require_once dirname(__DIR__) . '/lib/OneTimeDownloadStore.php';
ContactSpamGuard::startSession();
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');

$configFile = dirname(__DIR__) . '/config.php';
$config = is_file($configFile) ? require $configFile : [];
$siteUrl = rtrim((string)($config['site_url'] ?? 'https://hammertrackpro.com'), '/');
$installerPath = (string)($config['demo_installer_path'] ?? dirname(dirname(__DIR__)) . '/private/HammerTrackPro-Demo-x64.msi');
$storePath = (string)($config['demo_download_store'] ?? dirname(dirname(__DIR__)) . '/private/demo-download-tokens.json');
$installerReady = is_file($installerPath) && is_readable($installerPath);
$storageReady = is_dir(dirname($storePath)) && is_writable(dirname($storePath));
$csrf = (string)($_SESSION['download_admin_csrf'] ?? '');
if ($csrf === '') {
    $csrf = bin2hex(random_bytes(32));
    $_SESSION['download_admin_csrf'] = $csrf;
}

$error = '';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $submittedCsrf = (string)($_POST['csrf'] ?? '');
    $label = trim((string)($_POST['label'] ?? ''));
    $days = (int)($_POST['days'] ?? 7);
    $allowedDays = [1, 3, 7, 14];

    if (!hash_equals($csrf, $submittedCsrf)) {
        $error = 'The page session expired. Refresh and try again.';
    } elseif (!$installerReady || !$storageReady) {
        $error = 'The private installer or token-storage folder is not ready.';
    } elseif ($label === '') {
        $error = 'Enter the customer name or another identifying label.';
    } elseif (!in_array($days, $allowedDays, true)) {
        $error = 'Choose a valid expiration period.';
    } else {
        try {
            $now = time();
            $store = new OneTimeDownloadStore($storePath);
            $token = $store->create($label, $now + ($days * 86400), $now);
            $_SESSION['download_admin_flash'] = $siteUrl . '/download.php?token=' . rawurlencode($token);
            header('Location: index.php', true, 303);
            exit;
        } catch (Throwable $exception) {
            error_log('HammerTrack download admin: ' . $exception->getMessage());
            $error = 'The link could not be created. Check the Plesk private-folder permissions.';
        }
    }
}

$newLink = (string)($_SESSION['download_admin_flash'] ?? '');
unset($_SESSION['download_admin_flash']);
$recent = [];
if ($storageReady) {
    try {
        $recent = (new OneTimeDownloadStore($storePath))->recent(time());
    } catch (Throwable $exception) {
        error_log('HammerTrack download admin: ' . $exception->getMessage());
        $error = $error !== '' ? $error : 'Recent links could not be loaded.';
    }
}
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex,nofollow">
  <title>Demo Download Links | HammerTrack Pro</title>
  <link rel="stylesheet" href="../assets/styles.css?v=20261008">
</head>
<body class="download-admin-page">
  <main class="download-admin-shell">
    <header><p class="eyebrow">Private administration</p><h1>One-time demo download links</h1><p>Create a separate link for each customer. The first successful download permanently consumes that link.</p></header>

    <?php if (!$installerReady || !$storageReady): ?>
      <section class="admin-alert error"><strong>Setup required</strong><p><?= !$installerReady ? 'Upload HammerTrackPro-Demo-x64.msi to the domain’s private folder. ' : '' ?><?= !$storageReady ? 'Make sure the private folder is writable by the website’s PHP user.' : '' ?></p></section>
    <?php endif; ?>
    <?php if ($error !== ''): ?><section class="admin-alert error"><strong>Could not create link</strong><p><?= htmlspecialchars($error) ?></p></section><?php endif; ?>
    <?php if ($newLink !== ''): ?>
      <section class="admin-new-link"><p class="eyebrow">New link—copy it now</p><label>Customer download link<input id="generated-link" value="<?= htmlspecialchars($newLink, ENT_QUOTES) ?>" readonly></label><button class="button button-red" type="button" onclick="navigator.clipboard.writeText(document.getElementById('generated-link').value);this.textContent='Copied'">Copy link</button><p>This complete link is shown only once. The stored record contains only its secure hash.</p></section>
    <?php endif; ?>

    <section class="admin-generator">
      <h2>Create a download link</h2>
      <form method="post">
        <input type="hidden" name="csrf" value="<?= htmlspecialchars($csrf, ENT_QUOTES) ?>">
        <label>Customer or reference name<input name="label" required maxlength="120" autocomplete="off" placeholder="Example: Smith Auction Company"></label>
        <label>Expire if unused after<select name="days"><option value="1">1 day</option><option value="3">3 days</option><option value="7" selected>7 days</option><option value="14">14 days</option></select></label>
        <button class="button button-red" type="submit" <?= (!$installerReady || !$storageReady) ? 'disabled' : '' ?>>Generate one-time link</button>
      </form>
    </section>

    <section class="admin-history">
      <h2>Recent links</h2>
      <?php if ($recent === []): ?><p>No links have been generated yet.</p><?php else: ?>
      <div class="admin-table-wrap"><table><thead><tr><th>Customer/reference</th><th>Created</th><th>Expires</th><th>Status</th></tr></thead><tbody>
      <?php foreach ($recent as $record):
          $usedAt = is_int($record['used_at'] ?? null) ? $record['used_at'] : null;
          $expiresAt = is_int($record['expires_at'] ?? null) ? $record['expires_at'] : 0;
          $status = $usedAt !== null ? 'Downloaded' : ($expiresAt < time() ? 'Expired' : 'Available');
      ?>
        <tr><td><?= htmlspecialchars((string)($record['label'] ?? '')) ?></td><td><?= date('M j, Y g:i A', (int)($record['created_at'] ?? 0)) ?></td><td><?= date('M j, Y g:i A', $expiresAt) ?></td><td><span class="admin-status <?= strtolower($status) ?>"><?= $status ?></span></td></tr>
      <?php endforeach; ?>
      </tbody></table></div>
      <?php endif; ?>
    </section>
  </main>
</body>
</html>
