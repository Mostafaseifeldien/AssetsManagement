namespace AssetsManagement.Application;

public interface ICurrentUser
{
    string UserName { get; }
    string DisplayName { get; }
}
