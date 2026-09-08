namespace AssetsManagement.Api.Controllers;

public sealed record AssetAssignmentRequest(Guid AssetId);

public sealed record ReplacementRequest(Guid ReplacementId);
