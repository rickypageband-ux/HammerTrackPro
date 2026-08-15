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

expect(str_contains($html, 'Wholesale Auction upgrade'), 'Wholesale upgrade heading is rendered.');
expect(str_contains($html, '<span>$</span>299<span class="cents">.00</span>'), '$299.00 price is rendered.');
expect(str_contains($html, 'Request the upgrade — $299'), 'Missing checkout configuration uses the honest request fallback.');
expect(str_contains($html, 'Info@rpdavid.com'), 'The configured sales email is displayed.');
expect(str_contains($html, 'name="machine_id"'), 'Order request captures the Machine ID needed for activation.');
expect(str_contains($html, 'Offline, machine-bound activation key'), 'Activation terms are explained.');

fwrite(STDOUT, "Website tests passed.\n");
