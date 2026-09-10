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
            EncodingStandard = "GS1 SGTIN", Status = "Unassigned", MoreInformation = false
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
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RfidTagRequest.MoreInformation));
    }

    [Fact]
    public void Rfid_validator_allows_encoding_standard_to_be_omitted()
    {
        var result = new RfidTagRequestValidator().Validate(new RfidTagRequest
        {
            TagIdentifier = "E280:6894:100343", TagType = "Passive UHF", Status = "Unassigned",
            MoreInformation = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rfid_validator_requires_asset_when_assigned()
    {
        var result = new RfidTagRequestValidator().Validate(new RfidTagRequest
        {
            TagIdentifier = "E280:6894:100343", TagType = "Active", Status = "Assigned",
            MoreInformation = false
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RfidTagRequest.Asset));
    }

    [Fact]
    public void Barcode_validator_rejects_unsupported_symbology()
    {
        var result = new BarcodeRequestValidator().Validate(new BarcodeRequest
        {
            Value = "AST-0001", Symbology = "EAN13", MoreInformation = false
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
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(BarcodeRequest.MoreInformation));
    }

    [Fact]
    public void Barcode_validator_requires_subject_when_assigned()
    {
        var result = new BarcodeRequestValidator().Validate(new BarcodeRequest
        {
            Value = "BC-000508", Symbology = "Code128", Status = "Assigned", MoreInformation = false
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(BarcodeRequest.SubjectReference));
    }

    [Fact]
    public void Barcode_validator_accepts_unassigned_stock_label()
    {
        var result = new BarcodeRequestValidator().Validate(new BarcodeRequest
        {
            Value = "BC-000508", Symbology = "DataMatrix", Status = "Unassigned", MoreInformation = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_category_validator_requires_name_active_and_more_information()
    {
        var result = new AssetCategoryRequestValidator().Validate(new AssetCategoryRequest
        {
            Name = "", Code = "", Active = null, MoreInformation = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetCategoryRequest.Name));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(AssetCategoryRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetCategoryRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetCategoryRequest.MoreInformation));
    }

    [Fact]
    public void Asset_category_validator_accepts_empty_code()
    {
        var result = new AssetCategoryRequestValidator().Validate(new AssetCategoryRequest
        {
            Name = "IT Equipment", Code = "", Active = true, MoreInformation = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_category_validator_accepts_optional_more_information()
    {
        var result = new AssetCategoryRequestValidator().Validate(new AssetCategoryRequest
        {
            Name = "Network Equipment", Code = "NET", Active = true, MoreInformation = true,
            AlternateName = "معدات الشبكة", ParentCategory = "IT Equipment", AccountCode = "1520"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Manufacturer_validator_requires_name_active_and_more_information()
    {
        var result = new ManufacturerRequestValidator().Validate(new ManufacturerRequest
        {
            Name = "", Code = "", Active = null, MoreInformation = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.Name));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ManufacturerRequest.MoreInformation));
    }

    [Fact]
    public void Manufacturer_validator_accepts_empty_code()
    {
        var result = new ManufacturerRequestValidator().Validate(new ManufacturerRequest
        {
            Name = "Dell", Code = "", Active = true, MoreInformation = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Manufacturer_validator_accepts_optional_more_information()
    {
        var result = new ManufacturerRequestValidator().Validate(new ManufacturerRequest
        {
            Name = "Dell", Code = "DELL", Active = true, MoreInformation = true,
            AlternateName = "ديل", Country = "United States",
            SupportContact = "support@dell.com", Website = "https://www.dell.com"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_type_validator_requires_name_active_and_more_information()
    {
        var result = new AssetTypeRequestValidator().Validate(new AssetTypeRequest
        {
            Name = "", Code = "", Active = null, MoreInformation = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.Name));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.Code));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.RequiresSerialNumber));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.RequiresBarcode));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.RequiresRfidTag));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetTypeRequest.MoreInformation));
    }

    [Fact]
    public void Asset_type_validator_accepts_empty_code_and_optional_flags()
    {
        var result = new AssetTypeRequestValidator().Validate(new AssetTypeRequest
        {
            Name = "Laptop", Code = "", Active = true, MoreInformation = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_type_validator_accepts_optional_more_information()
    {
        var result = new AssetTypeRequestValidator().Validate(new AssetTypeRequest
        {
            Name = "Laptop", Code = "LAPTOP", RequiresSerialNumber = true,
            AssetCategory = "IT Equipment", DefaultStatus = "Working",
            Active = true, MoreInformation = true,
            AlternateName = "حاسوب محمول", RequiresRfidTag = true, RequiresBarcode = false,
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
            Name = "Laptop", Code = "LAPTOP", RequiresSerialNumber = true,
            Active = true, MoreInformation = false,
            AlternateName = "string", RequiresRfidTag = true, RequiresBarcode = true,
            PermittedStatusTransitions = "string", CustomAttributeSchema = "string",
            DefaultDepreciationMethod = "string", DefaultUsefulLife = 0,
            NumberingScheme = "string"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_model_validator_requires_name_manufacturer_active_and_more_information()
    {
        var result = new AssetModelRequestValidator().Validate(new AssetModelRequest
        {
            Name = "", Manufacturer = "", ModelNumber = "", Active = null, MoreInformation = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.Manufacturer));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.ModelNumber));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetModelRequest.MoreInformation));
    }

    [Fact]
    public void Asset_model_validator_accepts_empty_model_number()
    {
        var result = new AssetModelRequestValidator().Validate(new AssetModelRequest
        {
            Name = "Latitude 5540", Manufacturer = "Dell", ModelNumber = "",
            Active = true, MoreInformation = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_model_validator_skips_more_information_fields_when_no()
    {
        var result = new AssetModelRequestValidator().Validate(new AssetModelRequest
        {
            Name = "Latitude 5540", Manufacturer = "Dell", ModelNumber = "5540",
            Active = true, MoreInformation = false,
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
            Active = true, MoreInformation = true, ExpectedUsefulLife = 0
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
            IsOperational = true, IsTerminal = false, Active = true, MoreInformation = false
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
            IsOperational = true, IsTerminal = false, BlocksMovement = false,
            Active = true, MoreInformation = true, AlternateName = "يعمل"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_status_validator_accepts_unknown_with_trailing_period()
    {
        var result = new AssetStatusRequestValidator().Validate(new AssetStatusRequest
        {
            Code = "UNK", Name = "Unknown", StatusCategory = "Unknown.", Color = "#6b7280",
            IsOperational = false, IsTerminal = false, Active = true, MoreInformation = false
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
            Code = "SUP-001", Name = "Delta", Active = true, MoreInformation = true,
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
            Active = true, MoreInformation = false,
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
            Country = "Egypt", Active = true, MoreInformation = true,
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
            Asset = "Demo Asset", IsPrimary = true, MoreInformation = false, Purpose = "Portrait"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageRequest.Purpose));
    }

    [Fact]
    public void Asset_image_validator_accepts_prototype_photograph()
    {
        var result = new AssetImageRequestValidator().Validate(new AssetImageRequest
        {
            Asset = "Demo Asset", IsPrimary = true, Purpose = "Identification",
            MoreInformation = true, Caption = "Laptop 04405 — front"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Asset_image_create_validator_requires_asset_is_primary_and_more_information()
    {
        var result = new AssetImageCreateRequestValidator().Validate(new AssetImageCreateRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageCreateRequest.Asset));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageCreateRequest.IsPrimary));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AssetImageCreateRequest.MoreInformation));
    }

    [Fact]
    public void Asset_image_create_validator_accepts_add_photograph_modal()
    {
        var result = new AssetImageCreateRequestValidator().Validate(new AssetImageCreateRequest
        {
            Asset = "Demo Asset", IsPrimary = true, Purpose = "Nameplate",
            MoreInformation = true, Caption = "Serial nameplate"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Custom_attribute_validator_requires_screen_fields()
    {
        var result = new CustomAttributeDefinitionRequestValidator().Validate(new CustomAttributeDefinitionRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.AssetType));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.Label));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.DataType));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.EffectiveClass));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.Active));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.MoreInformation));
    }

    [Fact]
    public void Custom_attribute_validator_requires_list_values_for_list_type()
    {
        var result = new CustomAttributeDefinitionRequestValidator().Validate(new CustomAttributeDefinitionRequest
        {
            AssetType = "Laptop", Code = "disk", Label = "Storage", DataType = "List",
            EffectiveClass = "Optional", Active = "Yes", MoreInformation = "No"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.ListValues));
    }

    [Fact]
    public void Custom_attribute_validator_accepts_prototype_field()
    {
        var result = new CustomAttributeDefinitionRequestValidator().Validate(new CustomAttributeDefinitionRequest
        {
            AssetType = "Laptop", Code = "ram_gb", Label = "Memory", DataType = "Number",
            EffectiveClass = "Recommended", ShowInList = "Yes", Active = "Yes", MoreInformation = "Yes",
            AlternateName = "الذاكرة", Unit = "GB", HelpText = "Installed RAM in gigabytes."
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Custom_attribute_validator_accepts_yes_or_no_data_type()
    {
        var result = new CustomAttributeDefinitionRequestValidator().Validate(new CustomAttributeDefinitionRequest
        {
            AssetType = "Server", Code = "psu", Label = "Redundant power", DataType = "Yes or no",
            EffectiveClass = "Recommended", Active = "Yes", MoreInformation = "No"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Type_attribute_field_validator_requires_screen_fields()
    {
        var result = new TypeAttributeFieldRequestValidator().Validate(new TypeAttributeFieldRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(TypeAttributeFieldRequest.Code));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(TypeAttributeFieldRequest.Label));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(TypeAttributeFieldRequest.DataType));
        Assert.DoesNotContain(result.Errors, x => x.PropertyName == nameof(TypeAttributeFieldRequest.ShowInList));
    }

    [Fact]
    public void Type_attribute_field_validator_requires_two_list_values()
    {
        var result = new TypeAttributeFieldRequestValidator().Validate(new TypeAttributeFieldRequest
        {
            Code = "finish", Label = "Finish", DataType = "List", Class = "Optional",
            PossibleValues = ["Only one"]
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(TypeAttributeFieldRequest.PossibleValues));
    }

    [Fact]
    public void Type_attribute_field_validator_omits_possible_values_unless_list()
    {
        var result = new TypeAttributeFieldRequestValidator().Validate(new TypeAttributeFieldRequest
        {
            AssetType = "Laptop", Code = "notes", Label = "Notes", DataType = "Text", Class = "Optional"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Type_attribute_field_validator_accepts_prototype_field()
    {
        var result = new TypeAttributeFieldRequestValidator().Validate(new TypeAttributeFieldRequest
        {
            AssetType = "Laptop", Code = "memory_gb", Label = "Memory", AlternateName = "الذاكرة",
            DataType = "Number", Unit = "GB", Class = "Recommended", DisplayOrder = -2,
            HelpText = "Installed RAM in gigabytes."
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Type_attribute_field_validator_rejects_reference_data_type()
    {
        var result = new TypeAttributeFieldRequestValidator().Validate(new TypeAttributeFieldRequest
        {
            Code = "owner", Label = "Owner", DataType = "Reference", Class = "Optional"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(TypeAttributeFieldRequest.DataType));
    }

    [Fact]
    public void Custom_attribute_validator_rejects_unknown_data_type()
    {
        var result = new CustomAttributeDefinitionRequestValidator().Validate(new CustomAttributeDefinitionRequest
        {
            AssetType = "Laptop", Code = "notes", Label = "Notes", DataType = "Memo",
            EffectiveClass = "Optional", Active = "Yes", MoreInformation = "No"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomAttributeDefinitionRequest.DataType));
    }

    [Theory]
    [InlineData("YesNo", "Yes or no")]
    [InlineData("yes or no", "Yes or no")]
    [InlineData("Number", "Number")]
    [InlineData("list", "List")]
    public void Custom_attribute_data_type_aliases_match_screen(string input, string expected)
    {
        Assert.Equal(expected, CustomAttributeDefinitionRequestValidator.FormatDataType(input));
        Assert.True(CustomAttributeDefinitionRequestValidator.IsDataType(input));
    }
}
