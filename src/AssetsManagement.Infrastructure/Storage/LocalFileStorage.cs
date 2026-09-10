using AssetsManagement.Application;
using Microsoft.AspNetCore.Hosting;

namespace AssetsManagement.Infrastructure;

public sealed class LocalFileStorage(IWebHostEnvironment environment) : IFileStorageService
{
    private const string DefaultFolder = "asset-images";

    public Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken) =>
        SaveAsync(content, extension, DefaultFolder, cancellationToken);

    public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken cancellationToken)
    {
        var root = FolderRoot(folder);
        Directory.CreateDirectory(root);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(root, fileName);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(output, cancellationToken);
        return fileName;
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken) =>
        OpenReadAsync(storedFileName, DefaultFolder, cancellationToken);

    public Task<Stream> OpenReadAsync(string storedFileName, string folder, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(storedFileName);
        if (!string.Equals(safeName, storedFileName, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid stored file name.");
        Stream stream = new FileStream(Path.Combine(FolderRoot(folder), safeName), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken) =>
        DeleteAsync(storedFileName, DefaultFolder, cancellationToken);

    public Task DeleteAsync(string storedFileName, string folder, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(storedFileName);
        if (string.Equals(safeName, storedFileName, StringComparison.Ordinal))
        {
            var path = Path.Combine(FolderRoot(folder), safeName);
            if (File.Exists(path))
                File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private string FolderRoot(string folder)
    {
        var safe = Path.GetFileName(folder);
        if (string.IsNullOrWhiteSpace(safe) || !string.Equals(safe, folder, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage folder.");
        return Path.Combine(environment.ContentRootPath, "App_Data", safe);
    }
}
