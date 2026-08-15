<?php
declare(strict_types=1);
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { header('Location: /#contact', true, 303); exit; }
$configFile = __DIR__ . '/config.php';
$config = is_file($configFile) ? require $configFile : [];
$recipient = trim((string)($config['contact_email'] ?? ''));
function clean(string $name, int $limit): string {
    $value = trim((string)($_POST[$name] ?? ''));
    $value = str_replace(["\r", "\0"], '', $value);
    return substr($value, 0, $limit);
}
if (clean('website', 100) !== '') { http_response_code(204); exit; }
$name = clean('name', 100);
$email = filter_var(clean('email', 200), FILTER_VALIDATE_EMAIL);
$topic = clean('topic', 100);
$machineId = clean('machine_id', 200);
$message = clean('message', 3000);
if ($recipient === '' || $name === '' || $email === false || $message === '') {
    http_response_code(422);
    $title = $recipient === '' ? 'Contact form setup needed' : 'Please check your information';
    $detail = $recipient === '' ? 'The site owner needs to add the contact email in config.php.' : 'Name, a valid email, and a message are required.';
} else {
    $subject = '[HammerTrack Pro] ' . ($topic !== '' ? $topic : 'Website request');
    $body = "Name: {$name}\nEmail: {$email}\nTopic: {$topic}\nMachine ID: {$machineId}\n\nMessage:\n{$message}\n";
    $host = preg_replace('/[^a-z0-9.-]/i', '', (string)($_SERVER['SERVER_NAME'] ?? 'localhost'));
    $headers = ['From: HammerTrack Pro Website <no-reply@' . $host . '>', 'Reply-To: ' . $email, 'Content-Type: text/plain; charset=UTF-8'];
    $sent = mail($recipient, $subject, $body, implode("\r\n", $headers));
    http_response_code($sent ? 200 : 500);
    $title = $sent ? 'Request received' : 'We could not send your request';
    $detail = $sent ? 'Thank you. We will follow up using the email address you provided.' : 'Please try again later or contact HammerTrack Pro directly.';
}
?>
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title><?= htmlspecialchars($title) ?> | HammerTrack Pro</title><link rel="stylesheet" href="assets/styles.css"></head><body><main class="section"><div class="section-heading"><p class="eyebrow">HammerTrack Pro</p><h2><?= htmlspecialchars($title) ?></h2><p><?= htmlspecialchars($detail) ?></p><a class="button button-primary" href="/#contact">Return to the website</a></div></main></body></html>
