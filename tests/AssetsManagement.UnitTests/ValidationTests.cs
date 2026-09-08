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
            TagIdentifier = "EPC:3034-ABC_100", TagType = "Passive UHF", EncodingStandard = "GS1 SGTIN"
        });

        Assert.True(result.IsValid);
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
}
