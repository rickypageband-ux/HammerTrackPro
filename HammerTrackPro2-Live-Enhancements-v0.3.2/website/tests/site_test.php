<?php
declare(strict_types=1);

function expect(bool $condition, string $message): void
{
    if (!$condition) {
        fwrite(STDERR, "FAIL: {$message}\n");
        exit(1);
    }
}

ob_start();
require dirname(__DIR__) . '/index.php';
$html = (string)ob_get_clean();

expect(str_contains($html, 'assets/hammertrack-pro-logo.png'), 'The supplied HammerTrack Pro logo is used.');
expect(str_contains($html, 'Auction Software — $999'), '$999 Auction Software request option is rendered.');
expect(str_contains($html, 'Whole Package — $2,500'), '$2,500 Whole Package request option is rendered.');
expect(str_contains($html, 'Wholesale Auction upgrade — $299'), '$299 Wholesale upgrade request option is rendered.');
expect(str_contains($html, 'Info@rpdavid.com'), 'The configured sales email is displayed.');
expect(str_contains($html, 'name="machine_id"'), 'Order request captures the Machine ID needed for activation.');
expect(str_contains($html, 'name="website"'), 'The contact form includes a bot-trap field.');
expect(str_contains($html, 'name="contact_token"'), 'The contact form includes a one-time session token.');

ob_start();
require dirname(__DIR__) . '/pricing.php';
$pricing = (string)ob_get_clean();
expect(str_contains($pricing, '<sup>$</sup>999<span>.00</span>'), 'Auction Software is priced at $999.00.');
expect(str_contains($pricing, '<sup>$</sup>2,500<span>.00</span>'), 'Whole Package is priced at $2,500.00.');
expect(str_contains($pricing, '<li>2 laptops</li>'), 'Whole Package includes two laptops.');
expect(str_contains($pricing, '<li>1 laser printer</li>'), 'Whole Package includes a laser printer.');
expect(str_contains($pricing, '<li>1 NETUM scanner</li>'), 'Whole Package includes a NETUM scanner.');
expect(str_contains($pricing, '<li>1 network router</li>'), 'Whole Package includes a network router.');
expect(str_contains($pricing, 'On-site setup in South Carolina'), 'Whole Package includes South Carolina setup.');
expect(str_contains($pricing, '1 hour of training'), 'Whole Package includes one hour of training.');
expect(str_contains($pricing, 'outside South Carolina may require additional fees'), 'Out-of-state service fees are disclosed.');
expect(str_contains($pricing, '$199<span>.00/year</span>'), 'Yearly Updates are priced at $199.00.');
expect(str_contains($pricing, '$100<span>/hour</span>'), 'Hourly Support is priced at $100 per hour.');
expect(str_contains($pricing, 'TBD<span>per request</span>'), 'Feature requests are quoted per request.');

require_once dirname(__DIR__) . '/lib/SmtpMailer.php';
$incompleteConfigRejected = false;
try {
    SmtpMailer::send([
        'host' => 'smtp.ionos.com',
        'port' => 465,
        'encryption' => 'ssl',
        'username' => 'Info@rpdavid.com',
        'password' => 'ENTER_YOUR_IONOS_EMAIL_PASSWORD_HERE',
        'from_email' => 'Info@rpdavid.com'
    ], 'Info@rpdavid.com', 'customer@example.com', 'Test', 'Test');
} catch (RuntimeException) {
    $incompleteConfigRejected = true;
}
expect($incompleteConfigRejected, 'SMTP refuses to send while the password placeholder is present.');

require_once dirname(__DIR__) . '/lib/ContactSpamGuard.php';
$session = [];
$token = ContactSpamGuard::issueToken($session, 100);
expect(ContactSpamGuard::consumeToken($session, $token, 103) === 100, 'A valid contact token can be consumed once.');
expect(ContactSpamGuard::consumeToken($session, $token, 103) === null, 'A contact token cannot be reused.');
expect(ContactSpamGuard::hasValidTiming(100, 102), 'A human-speed form submission is accepted.');
expect(!ContactSpamGuard::hasValidTiming(100, 101), 'An immediate bot submission is rejected.');
expect(ContactSpamGuard::hasTooManyLinks('https://one.test www.two.test http://three.test'), 'Link-heavy messages are rejected.');
expect(!ContactSpamGuard::hasTooManyLinks('Please see https://example.com for our auction.'), 'A normal message may include a useful link.');

$rateFile = tempnam(sys_get_temp_dir(), 'hammertrack-rate-');
expect($rateFile !== false, 'A temporary rate-limit file can be created.');
if ($rateFile !== false) {
    for ($attempt = 0; $attempt < 5; $attempt++) {
        expect(ContactSpamGuard::allowRequest($rateFile, '192.0.2.1', 200 + $attempt), 'An allowed request stays below the hourly limit.');
    }
    expect(!ContactSpamGuard::allowRequest($rateFile, '192.0.2.1', 210), 'The sixth hourly request is blocked.');
    unlink($rateFile);
}

fwrite(STDOUT, "Website tests passed.\n");
