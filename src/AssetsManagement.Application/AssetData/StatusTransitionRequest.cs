namespace AssetsManagement.Application;

public sealed record StatusTransitionRequest(IReadOnlyCollection<Guid> AllowedToStatusIds);
