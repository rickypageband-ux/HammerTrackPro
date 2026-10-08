<?php
declare(strict_types=1);

final class OneTimeDownloadStore
{
    public function __construct(private readonly string $storageFile)
    {
    }

    public function create(string $label, int $expiresAt, int $now): string
    {
        [$handle, $records] = $this->loadLocked();

        try {
            $records = $this->removeOldRecords($records, $now);
            $token = bin2hex(random_bytes(32));
            $records[hash('sha256', $token)] = [
                'label' => substr(trim($label), 0, 120),
                'created_at' => $now,
                'expires_at' => $expiresAt,
                'used_at' => null,
            ];
            $this->save($handle, $records);
            return $token;
        } finally {
            flock($handle, LOCK_UN);
            fclose($handle);
        }
    }

    public function consume(string $token, int $now): string
    {
        if (!preg_match('/\A[a-f0-9]{64}\z/', $token)) {
            return 'invalid';
        }

        [$handle, $records] = $this->loadLocked();

        try {
            $key = hash('sha256', $token);
            $record = $records[$key] ?? null;
            if (!is_array($record)) {
                return 'invalid';
            }
            if (($record['used_at'] ?? null) !== null) {
                return 'used';
            }
            if (!is_int($record['expires_at'] ?? null) || $record['expires_at'] < $now) {
                return 'expired';
            }

            $records[$key]['used_at'] = $now;
            $this->save($handle, $records);
            return 'ok';
        } finally {
            flock($handle, LOCK_UN);
            fclose($handle);
        }
    }

    public function status(string $token, int $now): string
    {
        if (!preg_match('/\A[a-f0-9]{64}\z/', $token)) {
            return 'invalid';
        }

        [$handle, $records] = $this->loadLocked();

        try {
            $record = $records[hash('sha256', $token)] ?? null;
            if (!is_array($record)) {
                return 'invalid';
            }
            if (($record['used_at'] ?? null) !== null) {
                return 'used';
            }
            if (!is_int($record['expires_at'] ?? null) || $record['expires_at'] < $now) {
                return 'expired';
            }
            return 'ok';
        } finally {
            flock($handle, LOCK_UN);
            fclose($handle);
        }
    }

    public function recent(int $now, int $limit = 30): array
    {
        [$handle, $records] = $this->loadLocked();

        try {
            $records = $this->removeOldRecords($records, $now);
            $this->save($handle, $records);
            $items = array_values($records);
            usort($items, static fn (array $left, array $right): int => ($right['created_at'] ?? 0) <=> ($left['created_at'] ?? 0));
            return array_slice($items, 0, $limit);
        } finally {
            flock($handle, LOCK_UN);
            fclose($handle);
        }
    }

    private function loadLocked(): array
    {
        $directory = dirname($this->storageFile);
        if (!is_dir($directory) || !is_writable($directory)) {
            throw new RuntimeException('The private download-storage folder is missing or not writable.');
        }

        $handle = @fopen($this->storageFile, 'c+');
        if ($handle === false || !flock($handle, LOCK_EX)) {
            if (is_resource($handle)) {
                fclose($handle);
            }
            throw new RuntimeException('The one-time download store could not be opened.');
        }

        rewind($handle);
        $decoded = json_decode((string)stream_get_contents($handle), true);
        return [$handle, is_array($decoded) ? $decoded : []];
    }

    private function save($handle, array $records): void
    {
        rewind($handle);
        ftruncate($handle, 0);
        if (fwrite($handle, json_encode($records, JSON_THROW_ON_ERROR)) === false) {
            throw new RuntimeException('The one-time download store could not be saved.');
        }
        fflush($handle);
    }

    private function removeOldRecords(array $records, int $now): array
    {
        $retentionCutoff = $now - (30 * 86400);
        return array_filter($records, static function (mixed $record) use ($retentionCutoff): bool {
            if (!is_array($record)) {
                return false;
            }
            $usedAt = $record['used_at'] ?? null;
            $expiresAt = $record['expires_at'] ?? null;
            return (is_int($usedAt) && $usedAt >= $retentionCutoff)
                || ($usedAt === null && is_int($expiresAt) && $expiresAt >= $retentionCutoff);
        });
    }
}
