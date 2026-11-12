using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Application.Interfaces.Storage;
using Microsoft.Extensions.Options;

namespace IncludePresenca.Infrastructure.Storage;

public sealed class PrivateFileStorageOptions
{
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "private-attachments");
}

public sealed class PrivateFileStorage : IPrivateFileStorage
{
    private const long MaxLength = 5 * 1024 * 1024;
    private readonly string _rootPath;

    public PrivateFileStorage(IOptions<PrivateFileStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredPrivateFile> SaveAsync(AttachmentUpload file, CancellationToken cancellationToken = default)
    {
        if (file.Length is <= 0 or > MaxLength)
            throw new ArgumentException("Attachment must be between 1 byte and 5 MB.", nameof(file));

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string contentType = file.ContentType.ToLowerInvariant();
        string[] allowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
        string[] allowedTypes = ["application/pdf", "image/png", "image/jpeg"];
        if (!allowedExtensions.Contains(extension) || !allowedTypes.Contains(contentType))
            throw new ArgumentException("Attachment type is not supported.", nameof(file));

        await using MemoryStream buffer = new();
        await file.Content.CopyToAsync(buffer, cancellationToken);
        byte[] bytes = buffer.ToArray();
        if (bytes.LongLength is <= 0 or > MaxLength)
            throw new ArgumentException("Attachment content must be between 1 byte and 5 MB.", nameof(file));

        if (!HasValidSignature(bytes, extension))
            throw new ArgumentException("Attachment content does not match its declared type.", nameof(file));

        string key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        string fullPath = ResolveSafePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
        return new StoredPrivateFile(key, Path.GetFileName(file.FileName), contentType, bytes.LongLength);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        string fullPath = ResolveSafePath(storageKey);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string ResolveSafePath(string key)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_rootPath, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid storage key.", nameof(key));
        return fullPath;
    }

    private static bool HasValidSignature(byte[] bytes, string extension) => extension switch
    {
        ".pdf" => bytes.Length >= 5 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46 && bytes[4] == 0x2D,
        ".png" => bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E &&
            bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
        ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        _ => false
    };
}
