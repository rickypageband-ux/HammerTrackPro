<?php
declare(strict_types=1);

require_once __DIR__ . '/lib/OneTimeDownloadStore.php';

$configFile = __DIR__ . '/config.php';
$config = is_file($configFile) ? require $configFile : [];
$installerPath = (string)($config['demo_installer_path'] ?? dirname(__DIR__) . '/private/HammerTrackPro-Demo-x64.msi');
$storePath = (string)($config['demo_download_store'] ?? dirname(__DIR__) . '/private/demo-download-tokens.json');
$token = trim((string)($_REQUEST['token'] ?? ''));

if (!in_array($_SERVER['REQUEST_METHOD'], ['GET', 'POST'], true)) {
    http_response_code(405);
    header('Allow: GET, POST');
    exit;
}

$installer = null;
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $installer = @fopen($installerPath, 'rb');
    if ($installer === false) {
        error_log('HammerTrack demo download: installer is missing or unreadable.');
        renderDownloadMessage(503, 'Download temporarily unavailable', 'Please contact HammerTrack Pro and we will help you obtain the installer.');
    }
}

try {
    $store = new OneTimeDownloadStore($storePath);
    $result = $_SERVER['REQUEST_METHOD'] === 'POST'
        ? $store->consume($token, time())
        : $store->status($token, time());
} catch (Throwable $exception) {
    if (is_resource($installer)) {
        fclose($installer);
    }
    error_log('HammerTrack demo download: ' . $exception->getMessage());
    renderDownloadMessage(503, 'Download temporarily unavailable', 'Please contact HammerTrack Pro and we will help you obtain the installer.');
}

if ($result !== 'ok') {
    if (is_resource($installer)) {
        fclose($installer);
    }
    renderDownloadMessage(410, 'This download link is no longer available', 'Each demo installer link works once and may also expire. Request a new link from HammerTrack Pro.');
}

if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    renderDownloadConfirmation($token);
}

$size = filesize($installerPath);
header('Content-Type: application/octet-stream');
header('Content-Disposition: attachment; filename="HammerTrackPro-7-Day-Demo-x64.msi"');
header('Content-Transfer-Encoding: binary');
header('X-Content-Type-Options: nosniff');
header('Cache-Control: private, no-store, no-cache, must-revalidate, max-age=0');
header('Pragma: no-cache');
if ($size !== false) {
    header('Content-Length: ' . $size);
}
set_time_limit(0);

while (!feof($installer)) {
    $chunk = fread($installer, 1048576);
    if ($chunk === false) {
        break;
    }
    echo $chunk;
    flush();
}
fclose($installer);
exit;

function renderDownloadMessage(int $status, string $title, string $detail): never
{
    http_response_code($status);
    header('Content-Type: text/html; charset=utf-8');
    header('Cache-Control: no-store');
    ?>
    <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title><?= htmlspecialchars($title) ?> | HammerTrack Pro</title><link rel="stylesheet" href="assets/styles.css?v=20261008"></head><body><main class="section"><div class="section-heading"><p class="eyebrow">HammerTrack Pro</p><h2><?= htmlspecialchars($title) ?></h2><p><?= htmlspecialchars($detail) ?></p><a class="button button-red" href="demo.php">Return to the demo page</a></div></main></body></html>
    <?php
    exit;
}

function renderDownloadConfirmation(string $token): never
{
    header('Content-Type: text/html; charset=utf-8');
    header('Cache-Control: no-store');
    ?>
    <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="robots" content="noindex,nofollow"><title>Download HammerTrack Pro Demo</title><link rel="stylesheet" href="assets/styles.css?v=20261008"></head><body><main class="section"><div class="section-heading"><p class="eyebrow">Your private demo download</p><h2>HammerTrack Pro is ready.</h2><p>This link can download the installer one time. Press the button when you are ready to save the approximately 100 MB Windows installer.</p><form method="post"><input type="hidden" name="token" value="<?= htmlspecialchars($token, ENT_QUOTES) ?>"><button class="button button-red" type="submit">Download installer</button></form><p>Your separate signed demo key starts the seven-day evaluation period when HammerTrack Pro issues it.</p></div></main></body></html>
    <?php
    exit;
}
