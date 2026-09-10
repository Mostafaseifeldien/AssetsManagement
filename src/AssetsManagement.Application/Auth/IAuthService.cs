namespace AssetsManagement.Application;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResult(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    string Username,
    IReadOnlyCollection<string> Roles);

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
