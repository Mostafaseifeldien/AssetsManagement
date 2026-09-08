using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

[Route("api/suppliers")]
public sealed class SuppliersController(IAssetDataService service)
    : MasterDataController(service, AssetDataResource.Suppliers);
