using IncludePresenca.Application.DTOs.Justifications;

namespace IncludePresenca.Application.Interfaces.Storage;

public interface IPrivateFileStorage
{
    Task<StoredPrivateFile> SaveAsync(AttachmentUpload file, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record StoredPrivateFile(string StorageKey, string OriginalFileName, string ContentType, long Length);
