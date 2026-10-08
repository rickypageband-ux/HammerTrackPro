<?php
declare(strict_types=1);
return [
    // Inbox that receives sales and support requests.
    'contact_email' => 'sales@hammertrackpro.com',
    'site_url' => 'https://hammertrackpro.com',
    'demo_installer_path' => dirname(__DIR__) . '/private/HammerTrackPro-Demo-x64.msi',
    'demo_download_store' => dirname(__DIR__) . '/private/demo-download-tokens.json',
    // Optional hosted checkout/payment-link URL. Blank uses the request form.
    'wholesale_checkout_url' => '',
];
