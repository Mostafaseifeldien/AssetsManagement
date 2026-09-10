namespace AssetsManagement.Domain;

public sealed class Asset : CodedMasterEntity
{
    public string AssetNumber { get; set; } = "";
    public Guid AssetTypeId { get; set; }
    public AssetType AssetType { get; set; } = null!;
    public Guid? AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }
    public Guid? AssetModelId { get; set; }
    public AssetModel? AssetModel { get; set; }
    public Guid? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid AssetStatusId { get; set; }
    public AssetStatus AssetStatus { get; set; } = null!;
    public string? SerialNumber { get; set; }
    public string? OwningOrganization { get; set; }
    public string? OwningDepartment { get; set; }
    public string? CostCenter { get; set; }
    public Guid? CurrentCustodianId { get; set; }
    public Employee? CurrentCustodian { get; set; }
    public string? CustodianType { get; set; }
    public string? CurrentLocation { get; set; }
    public DateTime? LocationUpdatedAtUtc { get; set; }
    public string? LocationSource { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public string? LastSeenReader { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public string? PurchaseReference { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? DepreciationMethod { get; set; }
    public int? UsefulLifeMonths { get; set; }
    public decimal? ResidualValue { get; set; }
    public string? Criticality { get; set; }
    public Guid? ParentAssetId { get; set; }
    public Asset? ParentAsset { get; set; }
    public DateTime? CommissionedDate { get; set; }
    public DateTime? DisposalDate { get; set; }
    public string? DisposalReason { get; set; }
    public string? CustomAttributesJson { get; set; }
    public ICollection<AssetImage> Images { get; set; } = [];
    public ICollection<CustodyAssignment> CustodyAssignments { get; set; } = [];
    public ICollection<AssetDocumentLink> DocumentLinks { get; set; } = [];
    public ICollection<AssetRelationship> SourceRelationships { get; set; } = [];
    public ICollection<AssetRelationship> TargetRelationships { get; set; } = [];
}
