namespace AssetsManagement.Application;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}
