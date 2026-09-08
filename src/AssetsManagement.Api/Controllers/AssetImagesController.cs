using AssetsManagement.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

public sealed class ImageUploadRequest
{
    public required IFormFile File { get; init; }
    public string? Caption { get; init; }
    public string? Purpose { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed record ImageMetadataRequest(string? Caption, string? Purpose);

[ApiController]
[Route("api/asset-images")]
public sealed class AssetImagesController(IAssetDataService service) : ControllerBase
{
    private const long MaximumImageSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetImageDto>>>> List(
        [FromQuery] ListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetImageDto>>.Ok(await service.ListImagesAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDto>.Ok(await service.GetImageAsync(id, cancellationToken)));

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken cancellationToken)
    {
        var file = await service.OpenImageAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumImageSize)]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> Upload(
        [FromQuery] Guid assetId, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        await ValidateImageAsync(request.File, cancellationToken);
        await using var stream = request.File.OpenReadStream();
        var result = await service.UploadImageAsync(assetId, new ImageUpload
        {
            Content = stream, OriginalFileName = Path.GetFileName(request.File.FileName),
            ContentType = request.File.ContentType, SizeBytes = request.File.Length,
            Caption = request.Caption, Purpose = request.Purpose, IsPrimary = request.IsPrimary
        }, cancellationToken);
        return Created($"/api/asset-images/{result.Id}",
            ApiResponse<AssetImageDto>.Ok(result, "Image uploaded successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> Update(
        Guid id, ImageMetadataRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDto>.Ok(
            await service.UpdateImageAsync(id, request.Caption, request.Purpose, cancellationToken),
            "Image metadata updated successfully."));

    [HttpPost("{id:guid}/primary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetImageDto>>> SetPrimary(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDto>.Ok(await service.SetPrimaryImageAsync(id, cancellationToken),
            "Primary image selected successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteImageAsync(id, cancellationToken);
        return NoContent();
    }

    private static async Task ValidateImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaximumImageSize)
            throw new Domain.DomainRuleException("Image size must be between 1 byte and 10 MB.");
        var extension = Path.GetExtension(file.FileName);
        if (!Extensions.Contains(extension) || !ContentTypes.Contains(file.ContentType))
            throw new Domain.DomainRuleException("Only JPEG, PNG and WebP images are supported.");
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header, cancellationToken);
        var isJpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var isWebp = read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                     header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        if (!isJpeg && !isPng && !isWebp)
            throw new Domain.DomainRuleException("The file content does not match a supported image format.");
    }
}
