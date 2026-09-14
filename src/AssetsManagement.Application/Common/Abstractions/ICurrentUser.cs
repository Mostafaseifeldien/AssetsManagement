namespace AssetsManagement.Application;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string UserName { get; }
    string DisplayName { get; }
}
