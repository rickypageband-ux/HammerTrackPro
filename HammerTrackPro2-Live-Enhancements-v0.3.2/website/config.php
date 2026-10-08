<?php
declare(strict_types=1);

return [
    'contact_email' => 'sales@hammertrackpro.com',
    'site_url' => 'https://hammertrackpro.com',
    'demo_installer_path' => dirname(__DIR__) . '/private/HammerTrackPro-Demo-x64.msi',
    'demo_download_store' => dirname(__DIR__) . '/private/demo-download-tokens.json',

    // Keep blank while Wholesale upgrades are handled by request.
    // A Square payment link can be added here later.
    'wholesale_checkout_url' => '',
];
