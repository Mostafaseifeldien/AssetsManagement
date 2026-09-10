namespace AssetsManagement.Domain;

public sealed class Supplier : CodedMasterEntity
{
        public string SupplierKind { get; set; } = "";
    public string? TaxRegistration { get; set; }
    public string? ContactPerson { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Rating { get; set; }
    public string? ExternalIdentifier { get; set; }
    public bool MoreInformation { get; set; }
}
