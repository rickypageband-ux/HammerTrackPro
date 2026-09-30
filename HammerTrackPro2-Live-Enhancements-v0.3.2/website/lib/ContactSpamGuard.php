<?php
declare(strict_types=1);

final class ContactSpamGuard
{
    private const SESSION_KEY = 'hammertrack_contact_forms';
    private const SESSION_ATTEMPTS_KEY = 'hammertrack_contact_attempts';

    public static function startSession(): void
    {
        if (session_status() === PHP_SESSION_ACTIVE) {
            return;
        }

        $https = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== '' && $_SERVER['HTTPS'] !== 'off';
        session_set_cookie_params([
            'lifetime' => 0,
            'path' => '/',
            'secure' => $https,
            'httponly' => true,
            'samesite' => 'Lax',
        ]);
        session_start();
    }

    public static function issueToken(array &$session, int $now): string
    {
        $forms = self::activeForms($session, $now);
        $token = bin2hex(random_bytes(32));
        $forms[$token] = $now;
        $session[self::SESSION_KEY] = array_slice($forms, -5, null, true);

        return $token;
    }

    public static function consumeToken(array &$session, string $token, int $now): ?int
    {
        $forms = self::activeForms($session, $now);
        $started = isset($forms[$token]) && is_int($forms[$token]) ? $forms[$token] : null;
        unset($forms[$token]);
        $session[self::SESSION_KEY] = $forms;

        return $started;
    }

    public static function hasValidTiming(?int $started, int $now, int $minimumSeconds = 2, int $maximumSeconds = 7200): bool
    {
        if ($started === null) {
            return false;
        }

        $elapsed = $now - $started;
        return $elapsed >= $minimumSeconds && $elapsed <= $maximumSeconds;
    }

    public static function hasTooManyLinks(string $text, int $maximumLinks = 2): bool
    {
        $count = preg_match_all('~(?:https?://|www\.)~i', $text);
        return $count !== false && $count > $maximumLinks;
    }

    public static function allowSessionRequest(array &$session, int $now, int $limit = 5, int $windowSeconds = 3600): bool
    {
        $attempts = $session[self::SESSION_ATTEMPTS_KEY] ?? [];
        if (!is_array($attempts)) {
            $attempts = [];
        }

        $cutoff = $now - $windowSeconds;
        $attempts = array_values(array_filter(
            $attempts,
            static fn (mixed $timestamp): bool => is_int($timestamp) && $timestamp > $cutoff
        ));

        if (count($attempts) >= $limit) {
            $session[self::SESSION_ATTEMPTS_KEY] = $attempts;
            return false;
        }

        $attempts[] = $now;
        $session[self::SESSION_ATTEMPTS_KEY] = $attempts;
        return true;
    }

    public static function allowRequest(string $storageFile, string $clientKey, int $now, int $limit = 5, int $windowSeconds = 3600): bool
    {
        $handle = @fopen($storageFile, 'c+');
        if ($handle === false) {
            error_log('HammerTrack contact form: rate-limit storage could not be opened.');
            return true;
        }

        try {
            if (!flock($handle, LOCK_EX)) {
                error_log('HammerTrack contact form: rate-limit storage could not be locked.');
                return true;
            }

            rewind($handle);
            $decoded = json_decode((string)stream_get_contents($handle), true);
            $records = is_array($decoded) ? $decoded : [];
            $cutoff = $now - $windowSeconds;

            foreach ($records as $key => $timestamps) {
                if (!is_array($timestamps)) {
                    unset($records[$key]);
                    continue;
                }

                $records[$key] = array_values(array_filter(
                    $timestamps,
                    static fn (mixed $timestamp): bool => is_int($timestamp) && $timestamp > $cutoff
                ));
                if ($records[$key] === []) {
                    unset($records[$key]);
                }
            }

            $key = hash('sha256', $clientKey);
            $attempts = $records[$key] ?? [];
            $allowed = count($attempts) < $limit;
            if ($allowed) {
                $attempts[] = $now;
                $records[$key] = $attempts;
            }

            rewind($handle);
            ftruncate($handle, 0);
            fwrite($handle, json_encode($records, JSON_THROW_ON_ERROR));
            fflush($handle);

            return $allowed;
        } catch (Throwable $exception) {
            error_log('HammerTrack contact form: rate-limit check failed: ' . $exception->getMessage());
            return true;
        } finally {
            flock($handle, LOCK_UN);
            fclose($handle);
        }
    }

    private static function activeForms(array $session, int $now): array
    {
        $forms = $session[self::SESSION_KEY] ?? [];
        if (!is_array($forms)) {
            return [];
        }

        return array_filter(
            $forms,
            static fn (mixed $started): bool => is_int($started) && $started >= $now - 7200
        );
    }
}
