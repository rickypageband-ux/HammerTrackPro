<?php
declare(strict_types=1);

final class SmtpMailer
{
    public static function send(array $config, string $recipient, string $replyTo, string $subject, string $body): void
    {
        $host = trim((string)($config['host'] ?? ''));
        $port = (int)($config['port'] ?? 465);
        $username = trim((string)($config['username'] ?? ''));
        $password = (string)($config['password'] ?? '');
        $fromEmail = trim((string)($config['from_email'] ?? $username));
        $fromName = trim((string)($config['from_name'] ?? 'HammerTrack Pro Website'));

        if ($host === '' || $username === '' || $password === '' || $password === 'ENTER_YOUR_IONOS_EMAIL_PASSWORD_HERE') {
            throw new RuntimeException('The private IONOS email settings have not been completed.');
        }
        foreach ([$username, $fromEmail, $recipient, $replyTo] as $address) {
            if (!filter_var($address, FILTER_VALIDATE_EMAIL)) {
                throw new RuntimeException('An email address in the SMTP configuration is invalid.');
            }
        }

        $transport = strtolower((string)($config['encryption'] ?? 'ssl')) === 'ssl' ? 'ssl://' : 'tcp://';
        $socket = @stream_socket_client($transport . $host . ':' . $port, $errorNumber, $errorMessage, 15, STREAM_CLIENT_CONNECT);
        if (!is_resource($socket)) {
            throw new RuntimeException('Could not connect to the IONOS email server.');
        }

        stream_set_timeout($socket, 15);
        try {
            self::expect($socket, [220]);
            self::command($socket, 'EHLO hammertrackpro.com', [250]);

            if ($transport === 'tcp://' && strtolower((string)($config['encryption'] ?? '')) === 'starttls') {
                self::command($socket, 'STARTTLS', [220]);
                if (!stream_socket_enable_crypto($socket, true, STREAM_CRYPTO_METHOD_TLS_CLIENT)) {
                    throw new RuntimeException('Could not start an encrypted connection to IONOS.');
                }
                self::command($socket, 'EHLO hammertrackpro.com', [250]);
            }

            self::command($socket, 'AUTH LOGIN', [334]);
            self::command($socket, base64_encode($username), [334]);
            self::command($socket, base64_encode($password), [235]);
            self::command($socket, 'MAIL FROM:<' . $fromEmail . '>', [250]);
            self::command($socket, 'RCPT TO:<' . $recipient . '>', [250, 251]);
            self::command($socket, 'DATA', [354]);

            $safeSubject = trim(str_replace(["\r", "\n"], '', $subject));
            $safeFromName = trim(str_replace(["\r", "\n"], '', $fromName));
            $normalizedBody = str_replace(["\r\n", "\r"], "\n", $body);
            $normalizedBody = preg_replace('/^\./m', '..', $normalizedBody) ?? $normalizedBody;
            $headers = [
                'From: ' . $safeFromName . ' <' . $fromEmail . '>',
                'To: <' . $recipient . '>',
                'Reply-To: <' . $replyTo . '>',
                'Subject: ' . $safeSubject,
                'MIME-Version: 1.0',
                'Content-Type: text/plain; charset=UTF-8',
                'Content-Transfer-Encoding: 8bit'
            ];
            fwrite($socket, implode("\r\n", $headers) . "\r\n\r\n" . str_replace("\n", "\r\n", $normalizedBody) . "\r\n.\r\n");
            self::expect($socket, [250]);
            self::command($socket, 'QUIT', [221]);
        } finally {
            fclose($socket);
        }
    }

    private static function command($socket, string $command, array $expected): void
    {
        fwrite($socket, $command . "\r\n");
        self::expect($socket, $expected);
    }

    private static function expect($socket, array $expected): void
    {
        $response = '';
        do {
            $line = fgets($socket, 515);
            if ($line === false) {
                throw new RuntimeException('The IONOS email server stopped responding.');
            }
            $response .= $line;
        } while (isset($line[3]) && $line[3] === '-');

        $status = (int)substr($response, 0, 3);
        if (!in_array($status, $expected, true)) {
            error_log('HammerTrack SMTP error: ' . trim($response));
            throw new RuntimeException('IONOS rejected the email request. Check the mailbox username and password.');
        }
    }
}
