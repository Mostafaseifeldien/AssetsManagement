using AssetsManagement.Application;
using AssetsManagement.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetsManagement.Api.Controllers;

public sealed class DocumentUploadRequest
{
    public required IFormFile File { get; init; }
    public string? AssetIds { get; init; }
    public string? DocumentKind { get; init; }
    public string? Title { get; init; }
    public DateTime? DocumentDate { get; init; }
    public DateTime? ExpiresOn { get; init; }
    public string? IssuedBy { get; init; }
    public string? ReferenceNumber { get; init; }
    public bool? Confidential { get; init; }
    public decimal? Amount { get; init; }
}

public sealed class DocumentUploadRequestValidator : AbstractValidator<DocumentUploadRequest>
{
    public DocumentUploadRequestValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("A document file is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DocumentKind).NotEmpty();
        RuleFor(x => x.Confidential).NotNull().WithMessage("Confidential is required.");
    }
}

[ApiController]
[Route("api/asset-documents")]
public sealed class AssetDocumentsController(IAssetDocumentService service) : ControllerBase
{
    private const long MaximumDocumentSize = 20 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".xls", ".xlsx", ".doc", ".docx", ".txt" };

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetDocumentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetDocumentListItemDto>>>> List(
        [FromQuery] AssetDocumentListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AssetDocumentListItemDto>>.Ok(
            await service.ListAsync(query, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDocumentDetailDto>>> Get(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDocumentDetailDto>.Ok(
            await service.GetAsync(id, cancellationToken), "Record retrieved successfully."));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssetDocumentHistoryDto>>>> History(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<AssetDocumentHistoryDto>>.Ok(
            await service.GetHistoryAsync(id, cancellationToken), "History retrieved successfully."));

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken cancellationToken)
    {
        var file = await service.OpenAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumDocumentSize)]
    public async Task<ActionResult<ApiResponse<AssetDocumentDetailDto>>> Create(
        [FromForm] DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        ValidateFile(request.File);
        await using var stream = request.File.OpenReadStream();
        var result = await service.CreateAsync(ToCreateRequest(request), ToUpload(request.File, stream), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id },
            ApiResponse<AssetDocumentDetailDto>.Ok(result, "Document uploaded successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDocumentDetailDto>>> Update(
        Guid id, AssetDocumentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDocumentDetailDto>.Ok(
            await service.UpdateAsync(id, request, cancellationToken), "Record updated successfully."));

    [HttpPost("{id:guid}/supersede")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumDocumentSize)]
    public async Task<ActionResult<ApiResponse<AssetDocumentDetailDto>>> Supersede(
        Guid id, [FromForm] DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        ValidateFile(request.File);
        await using var stream = request.File.OpenReadStream();
        var result = await service.SupersedeAsync(id, ToCreateRequest(request), ToUpload(request.File, stream), cancellationToken);
        return Ok(ApiResponse<AssetDocumentDetailDto>.Ok(result,
            "Document superseded. The previous version remains retrievable."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDocumentDetailDto>>> Restore(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AssetDocumentDetailDto>.Ok(
            await service.RestoreAsync(id, cancellationToken), "Record restored successfully."));

    private static AssetDocumentCreateRequest ToCreateRequest(DocumentUploadRequest request) => new()
    {
        AssetIds = ParseAssetIds(request.AssetIds),
        DocumentKind = request.DocumentKind,
        Title = request.Title,
        DocumentDate = request.DocumentDate,
        ExpiresOn = request.ExpiresOn,
        IssuedBy = request.IssuedBy,
        ReferenceNumber = request.ReferenceNumber,
        Confidential = request.Confidential,
        Amount = request.Amount
    };

    private static DocumentUpload ToUpload(IFormFile file, Stream stream) => new()
    {
        Content = stream,
        OriginalFileName = Path.GetFileName(file.FileName),
        ContentType = file.ContentType,
        SizeBytes = file.Length
    };

    private static IReadOnlyCollection<Guid> ParseAssetIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToArray();
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length is <= 0 or > MaximumDocumentSize)
            throw new DomainRuleException("Document size must be between 1 byte and 20 MB.");
        var extension = Path.GetExtension(file.FileName);
        if (!Extensions.Contains(extension))
            throw new DomainRuleException("That document type is not supported.");
    }
}
