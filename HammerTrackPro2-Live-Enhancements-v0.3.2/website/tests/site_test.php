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

fwrite(STDOUT, "Website tests passed.\n");
