using AssetsManagement.Application;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

public sealed class ImageUploadRequest
{
    public required IFormFile File { get; init; }
    public string? Asset { get; init; }
    public bool? IsPrimary { get; init; }
    public string? Purpose { get; init; }
    public bool? MoreInformation { get; init; }
    public string? Caption { get; init; }
}

public sealed class ImageUploadRequestValidator : AbstractValidator<ImageUploadRequest>
{
    public ImageUploadRequestValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("A photograph file is required.");
        RuleFor(x => x.IsPrimary).NotNull().WithMessage("Is primary is required.");
        RuleFor(x => x.MoreInformation).NotNull().WithMessage("More information is required.");
        RuleFor(x => x.Purpose).Must(AssetImageRequestValidator.IsPurpose)
            .When(x => !string.IsNullOrWhiteSpace(x.Purpose))
            .WithMessage("Purpose must be Identification, Condition record, Damage evidence or Nameplate.");
        RuleFor(x => x.Caption).MaximumLength(500)
            .When(x => x.MoreInformation == true);
    }
}

[ApiController]
[Route("api/asset-images")]
public sealed class AssetImagesController(IAssetImageService service) : ControllerBase
{
    private const long MaximumImageSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetImageListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetImageListItemDto>>>> List(
        [FromQuery] AssetImageListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetImageListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetImageDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetImageHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetImageHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken cancellationToken)
    {
        var file = await service.OpenAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumImageSize)]
    public async Task<ActionResult<ApiResponse<AssetImageDetailDto>>> Create(
        [FromQuery] Guid? assetId, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        await ValidateImageAsync(request.File, cancellationToken);
        await using var stream = request.File.OpenReadStream();
        var result = await service.CreateAsync(new AssetImageCreateRequest
        {
            Asset = FirstNonEmpty(request.Asset, assetId?.ToString()),
            IsPrimary = request.IsPrimary,
            Purpose = request.Purpose,
            MoreInformation = request.MoreInformation,
            Caption = request.Caption
        }, new ImageUpload
        {
            Content = stream,
            OriginalFileName = Path.GetFileName(request.File.FileName),
            ContentType = request.File.ContentType,
            SizeBytes = request.File.Length
        }, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetImageDetailDto>.Ok(result, "Photograph added to the asset."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetImageDetailDto>>> Update(
        Guid id, AssetImageRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDetailDto>.Ok(
            await service.UpdateAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/primary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetImageDetailDto>>> SetPrimary(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDetailDto>.Ok(
            await service.SetPrimaryAsync(id, cancellationToken),
            "Primary image selected successfully."));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetImageDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetImageDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));

    private static string? FirstNonEmpty(string? value, string? fallback) =>
        !string.IsNullOrWhiteSpace(value) ? value : fallback;

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
