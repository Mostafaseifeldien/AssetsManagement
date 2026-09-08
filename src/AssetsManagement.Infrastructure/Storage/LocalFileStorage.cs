using AssetsManagement.Application;
using Microsoft.AspNetCore.Hosting;

namespace AssetsManagement.Infrastructure;

public sealed class LocalFileStorage(IWebHostEnvironment environment) : IFileStorageService
{
    private string Root => Path.Combine(environment.ContentRootPath, "App_Data", "asset-images");

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Root);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(Root, fileName);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(output, cancellationToken);
        return fileName;
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(storedFileName);
        if (!string.Equals(safeName, storedFileName, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid stored file name.");
        Stream stream = new FileStream(Path.Combine(Root, safeName), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(storedFileName);
        if (string.Equals(safeName, storedFileName, StringComparison.Ordinal))
            File.Delete(Path.Combine(Root, safeName));
        return Task.CompletedTask;
    }
}
