using AssetsManagement.Application;
using AssetsManagement.Domain;

namespace AssetsManagement.UnitTests;

public sealed class ValidationTests
{
    [Fact]
    public void Master_validator_rejects_invalid_code_and_name()
    {
        var result = new MasterDataRequestValidator().Validate(new MasterDataRequest
        {
            Code = "not valid code", Name = ""
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(MasterDataRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(MasterDataRequest.Name));
    }

    [Fact]
    public void Rfid_validator_accepts_epc_style_identifier()
    {
        var result = new RfidTagRequestValidator().Validate(new RfidTagRequest
        {
            TagIdentifier = "EPC:3034-ABC_100", TagType = "Passive UHF",
            EncodingStandard = "GS1 SGTIN", Status = "Unassigned"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rfid_validator_requires_identifier_type_and_status()
    {
        var result = new RfidTagRequestValidator().Validate(new RfidTagRequest
        {
            TagIdentifier = "", TagType = "", Status = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RfidTagRequest.TagIdentifier));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RfidTagRequest.TagType));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RfidTagRequest.Status));
    }

    [Fact]
    public void Rfid_validator_allows_encoding_standard_to_be_omitted()
    {
        var result = new RfidTagRequestValidator().Validate(new RfidTagRequest
        {
            TagIdentifier = "E280:6894:100343", TagType = "Passive UHF", Status = "Unassigned"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rfid_validator_requires_asset_when_assigned()
    {
        var result = new RfidTagRequestValidator().Validate(new RfidTagRequest
        {
            TagIdentifier = "E280:6894:100343", TagType = "Active", Status = "Assigned"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RfidTagRequest.Asset));
    }

    [Fact]
    public void Barcode_validator_rejects_unsupported_symbology()
    {
        var result = new BarcodeRequestValidator().Validate(new BarcodeRequest
        {
            Value = "AST-0001", Symbology = "EAN13"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Barcode_validator_requires_value_symbology_and_status()
    {
        var result = new BarcodeRequestValidator().Validate(new BarcodeRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(BarcodeRequest.Value));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(BarcodeRequest.Symbology));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(BarcodeRequest.Status));
    }

    [Fact]
    public void Barcode_validator_requires_subject_when_assigned()
    {
        var result = new BarcodeRequestValidator().Validate(new BarcodeRequest
        {
            Value = "BC-000508", Symbology = "Code128", Status = "Assigned"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(BarcodeRequest.SubjectReference));
    }

    [Fact]
    public void Barcode_validator_accepts_unassigned_stock_label()
    {
        var result = new BarcodeRequestValidator().Validate(new BarcodeRequest
        {
            Value = "BC-000508", Symbology = "DataMatrix", Status = "Unassigned"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_category_validator_requires_name_and_active()
    {
        var result = new AssetCategoryRequestValidator().Validate(new AssetCategoryRequest
        {
            Name = "", Active = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetCategoryRequest.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetCategoryRequest.Active));
    }

    [Fact]
    public void Asset_category_validator_accepts_optional_more_information()
    {
        var result = new AssetCategoryRequestValidator().Validate(new AssetCategoryRequest
        {
            Name = "Network Equipment", Code = "NET", Active = true,
            AlternateName = "معدات الشبكة", ParentCategory = "IT Equipment", AccountCode = "1520"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Manufacturer_validator_requires_name_code_active_and_more_information()
    {
        var result = new ManufacturerRequestValidator().Validate(new ManufacturerRequest
        {
            Name = "", Code = "", Active = null, MoreInformation = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.MoreInformation));
    }

    [Fact]
    public void Manufacturer_validator_accepts_optional_more_information()
    {
        var result = new ManufacturerRequestValidator().Validate(new ManufacturerRequest
        {
            Name = "Dell", Code = "DELL", Active = "Yes", MoreInformation = "Yes",
            AlternateName = "ديل", Country = "United States",
            SupportContact = "support@dell.com", Website = "https://www.dell.com"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_type_validator_requires_name_code_active_and_more_information()
    {
        var result = new AssetTypeRequestValidator().Validate(new AssetTypeRequest
        {
            Name = "", Code = "", Active = null, MoreInformation = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.RequiresSerialNumber));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.MoreInformation));
    }

    [Fact]
    public void Asset_type_validator_accepts_optional_more_information()
    {
        var result = new AssetTypeRequestValidator().Validate(new AssetTypeRequest
        {
            Name = "Laptop", Code = "LAPTOP", RequiresSerialNumber = "Yes",
            AssetCategory = "IT Equipment", DefaultStatus = "Working",
            Active = "Yes", MoreInformation = "Yes",
            AlternateName = "حاسوب محمول", RequiresRfidTag = "Yes", RequiresBarcode = "No",
            DefaultDepreciationMethod = "Straight line", DefaultUsefulLife = 36,
            NumberingScheme = "LAP-#####"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_type_validator_skips_more_information_fields_when_no()
    {
        var result = new AssetTypeRequestValidator().Validate(new AssetTypeRequest
        {
            Name = "Laptop", Code = "LAPTOP", RequiresSerialNumber = "Yes",
            Active = "Yes", MoreInformation = "No",
            AlternateName = "string", RequiresRfidTag = "string", RequiresBarcode = "string",
            PermittedStatusTransitions = "string", CustomAttributeSchema = "string",
            DefaultDepreciationMethod = "string", DefaultUsefulLife = 0,
            NumberingScheme = "string"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_model_validator_requires_name_manufacturer_model_number_and_active()
    {
        var result = new AssetModelRequestValidator().Validate(new AssetModelRequest
        {
            Name = "", Manufacturer = "", ModelNumber = "", Active = null, MoreInformation = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.Manufacturer));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.ModelNumber));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.MoreInformation));
    }

    [Fact]
    public void Asset_model_validator_skips_more_information_fields_when_no()
    {
        var result = new AssetModelRequestValidator().Validate(new AssetModelRequest
        {
            Name = "Latitude 5540", Manufacturer = "Dell", ModelNumber = "5540",
            Active = "Yes", MoreInformation = "No",
            AlternateName = "string", AssetType = "string", Specifications = "string",
            ExpectedUsefulLife = 0, Documentation = "string"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_model_validator_checks_more_information_fields_when_yes()
    {
        var result = new AssetModelRequestValidator().Validate(new AssetModelRequest
        {
            Name = "Latitude 5540", Manufacturer = "Dell", ModelNumber = "5540",
            Active = "Yes", MoreInformation = "Yes", ExpectedUsefulLife = 0
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.ExpectedUsefulLife));
    }

    [Fact]
    public void Asset_status_validator_requires_screen_fields()
    {
        var result = new AssetStatusRequestValidator().Validate(new AssetStatusRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.StatusCategory));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.Color));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.IsOperational));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.IsTerminal));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.MoreInformation));
    }

    [Fact]
    public void Asset_status_validator_rejects_invalid_category_and_color()
    {
        var result = new AssetStatusRequestValidator().Validate(new AssetStatusRequest
        {
            Code = "WRK", Name = "Working", StatusCategory = "Broken", Color = "green",
            IsOperational = "Yes", IsTerminal = "No", Active = "Yes", MoreInformation = "No"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.StatusCategory));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetStatusRequest.Color));
    }

    [Fact]
    public void Asset_status_validator_accepts_prototype_status()
    {
        var result = new AssetStatusRequestValidator().Validate(new AssetStatusRequest
        {
            Code = "WRK", Name = "Working", StatusCategory = "Working", Color = "#16a34a",
            IsOperational = "Yes", IsTerminal = "No", BlocksMovement = "No",
            Active = "Yes", MoreInformation = "Yes", AlternateName = "يعمل"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_status_validator_accepts_unknown_with_trailing_period()
    {
        var result = new AssetStatusRequestValidator().Validate(new AssetStatusRequest
        {
            Code = "UNK", Name = "Unknown", StatusCategory = "Unknown.", Color = "#6b7280",
            IsOperational = "No", IsTerminal = "No", Active = "Yes", MoreInformation = "No"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Supplier_validator_requires_code_name_active_and_more_information()
    {
        var result = new SupplierRequestValidator().Validate(new SupplierRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SupplierRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SupplierRequest.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SupplierRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SupplierRequest.MoreInformation));
    }

    [Fact]
    public void Supplier_validator_rejects_invalid_kind_rating_and_email()
    {
        var result = new SupplierRequestValidator().Validate(new SupplierRequest
        {
            Code = "SUP-001", Name = "Delta", Active = "Yes", MoreInformation = "Yes",
            SupplierKind = "General", Rating = "Gold", Email = "not-an-email"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SupplierRequest.SupplierKind));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SupplierRequest.Rating));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SupplierRequest.Email));
    }

    [Fact]
    public void Supplier_validator_skips_optional_fields_when_more_information_is_no()
    {
        var result = new SupplierRequestValidator().Validate(new SupplierRequest
        {
            Code = "SUP-001", Name = "Delta Technology Distribution",
            Active = "Yes", MoreInformation = "No",
            SupplierKind = "Vendor", Email = "sales@deltatech.example",
            Rating = "not-a-rating", TaxRegistration = new string('x', 200)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Supplier_validator_accepts_prototype_supplier()
    {
        var result = new SupplierRequestValidator().Validate(new SupplierRequest
        {
            Code = "SUP-001", Name = "Delta Technology Distribution",
            SupplierKind = "Vendor", ContactPerson = "H. Farouk",
            Telephone = "+20 2 2735 4410", Email = "sales@deltatech.example",
            Country = "Egypt", Active = "Yes", MoreInformation = "Yes",
            AlternateName = "دلتا لتوزيع التقنية", TaxRegistration = "311-442-889",
            PaymentTerms = "30 days net", Rating = "Preferred", ExternalIdentifier = "ODOO-RP-1041"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_image_validator_requires_asset_and_is_primary()
    {
        var result = new AssetImageRequestValidator().Validate(new AssetImageRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageRequest.Asset));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageRequest.IsPrimary));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageRequest.MoreInformation));
    }

    [Fact]
    public void Asset_image_validator_rejects_unknown_purpose()
    {
        var result = new AssetImageRequestValidator().Validate(new AssetImageRequest
        {
            Asset = "Demo Asset", IsPrimary = "Yes", MoreInformation = "No", Purpose = "Portrait"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageRequest.Purpose));
    }

    [Fact]
    public void Asset_image_validator_accepts_prototype_photograph()
    {
        var result = new AssetImageRequestValidator().Validate(new AssetImageRequest
        {
            Asset = "Demo Asset", IsPrimary = "Yes", Purpose = "Identification",
            MoreInformation = "Yes", Caption = "Laptop 04405 — front"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_image_create_validator_accepts_add_photograph_modal()
    {
        var result = new AssetImageCreateRequestValidator().Validate(new AssetImageCreateRequest
        {
            Asset = "Demo Asset", IsPrimary = "Yes", Purpose = "Nameplate",
            Caption = "Serial nameplate"
        });

        Assert.True(result.IsValid);
    }
}
