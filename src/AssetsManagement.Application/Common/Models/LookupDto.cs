namespace AssetsManagement.Application;

public sealed record LookupDto(Guid Id, string Code, string Name);

public sealed record AssetLookupDto(Guid Id, string AssetNumber, string Name);
