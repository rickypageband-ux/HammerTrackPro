using Microsoft.Extensions.Options;

namespace HammerTrackLive.Web.Services;

public sealed class LocalLotMediaStorage(IOptions<MediaStorageOptions> options) : ILotMediaStorage
{
    private static readonly IReadOnlyDictionary<string, (string Extension, string Kind)> AllowedTypes =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", "Image"),
            ["image/png"] = (".png", "Image"),
            ["image/webp"] = (".webp", "Image"),
            ["application/pdf"] = (".pdf", "Document")
        };
    private readonly MediaStorageOptions _options = options.Value;

    public async Task<StoredMedia> SaveAsync(Stream source, string contentType, string originalFileName, CancellationToken cancellationToken = default)
    {
        if (!AllowedTypes.TryGetValue(contentType, out var type))
            throw new InvalidOperationException("Only JPG, PNG, WebP, and PDF files are accepted.");
        if (string.IsNullOrWhiteSpace(_options.RootPath))
            throw new InvalidOperationException("Auction media storage is not configured.");

        Directory.CreateDirectory(_options.RootPath);
        var storageKey = $"{Guid.NewGuid():N}{type.Extension}";
        var path = ResolvePath(storageKey);
        await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        var firstChunk = true;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (firstChunk && !HasValidSignature(contentType, buffer.AsSpan(0, read)))
            {
                destination.Close();
                File.Delete(path);
                throw new InvalidOperationException("The file contents do not match the selected JPG, PNG, WebP, or PDF format.");
            }
            firstChunk = false;
            total += read;
            if (total > _options.MaximumFileBytes)
            {
                destination.Close();
                File.Delete(path);
                throw new InvalidOperationException($"The file exceeds the {_options.MaximumFileBytes / 1024 / 1024} MB limit.");
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (total == 0)
        {
            destination.Close();
            File.Delete(path);
            throw new InvalidOperationException("The uploaded file is empty.");
        }
        var safeName = originalFileName.Replace('\\', '/').Split('/').Last();
        return new StoredMedia(storageKey, safeName, contentType, total, type.Kind);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Stream? stream = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var fileName = Path.GetFileName(storageKey);
        if (!string.Equals(fileName, storageKey, StringComparison.Ordinal)) throw new InvalidOperationException("The media key is invalid.");
        return Path.Combine(_options.RootPath, fileName);
    }

    private static bool HasValidSignature(string contentType, ReadOnlySpan<byte> data)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF,
            "image/png" => data.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8),
            "application/pdf" => data.StartsWith("%PDF-"u8),
            _ => false
        };
    }
}
