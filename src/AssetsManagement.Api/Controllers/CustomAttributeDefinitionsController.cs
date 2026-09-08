using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[Route("api/custom-attribute-definitions")]
public sealed class CustomAttributeDefinitionsController(IAssetDataService service)
    : MasterDataController(service, AssetDataResource.CustomAttributeDefinitions);
