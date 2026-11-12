using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace IncludePresenca.Application.Tests.Services;

public class PrivateFileStorageTests
{
    [Fact]
    public async Task SaveAsync_StoresPdfWithGeneratedPrivateKey()
    {
        string root = Path.Combine(Path.GetTempPath(), "include-presenca-tests", Guid.NewGuid().ToString("N"));
        try
        {
            PrivateFileStorage storage = new(Options.Create(new PrivateFileStorageOptions { RootPath = root }));
            await using MemoryStream stream = new("%PDF-1.7"u8.ToArray());

            var stored = await storage.SaveAsync(new AttachmentUpload(stream, "comprovante.pdf", "application/pdf", stream.Length));

            Assert.EndsWith(".pdf", stored.StorageKey);
            Assert.DoesNotContain("comprovante.pdf", stored.StorageKey);
            Assert.True(File.Exists(Path.Combine(root, stored.StorageKey.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsPdfWithInvalidSignature()
    {
        string root = Path.Combine(Path.GetTempPath(), "include-presenca-tests", Guid.NewGuid().ToString("N"));
        try
        {
            PrivateFileStorage storage = new(Options.Create(new PrivateFileStorageOptions { RootPath = root }));
            await using MemoryStream stream = new("not a pdf"u8.ToArray());

            await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(
                new AttachmentUpload(stream, "comprovante.pdf", "application/pdf", stream.Length)));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
