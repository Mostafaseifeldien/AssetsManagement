using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AssetsManagement.Application;
using AssetsManagement.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssetsManagement.IntegrationTests;

public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_login_returns_bearer_token()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "Admin@123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Bearer", json.RootElement.GetProperty("data").GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Invalid_login_returns_unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Write_without_token_returns_unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/asset-types", ValidAssetType($"T-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_get_and_filter_asset_type()
    {
        await AuthorizeAsync();
        var code = $"TYPE-{Guid.NewGuid():N}";
        var create = await _client.PostAsJsonAsync("/api/asset-types", ValidAssetType(code));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var get = await _client.GetAsync($"/api/asset-types/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var list = await _client.GetAsync($"/api/asset-types?search={code}&pageNumber=1&pageSize=5");
        using var listed = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Equal(1, listed.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32());
        Assert.Equal(5, listed.RootElement.GetProperty("data").GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Validation_and_duplicate_conflict_are_reported()
    {
        await AuthorizeAsync();
        var invalid = await _client.PostAsJsonAsync("/api/manufacturers", new { code = "bad code", name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var code = $"MFR-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created,
            (await _client.PostAsJsonAsync("/api/manufacturers", ValidManufacturer(code))).StatusCode);
        var duplicate = await _client.PostAsJsonAsync("/api/manufacturers", ValidManufacturer(code));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Missing_record_returns_not_found()
    {
        var response = await _client.GetAsync($"/api/suppliers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Category_parent_relationship_is_saved()
    {
        await AuthorizeAsync();
        var parentName = $"IT Equipment {Guid.NewGuid():N}"[..28];
        var parentResponse = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name = parentName, code = $"IT{Guid.NewGuid():N}"[..10], active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, parentResponse.StatusCode);

        var child = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name = $"Network Equipment {Guid.NewGuid():N}"[..32],
            code = $"NET{Guid.NewGuid():N}"[..10],
            active = true,
            moreInformation = true,
            parentCategory = parentName,
            accountCode = "1520"
        });
        Assert.Equal(HttpStatusCode.Created, child.StatusCode);
        using var childJson = JsonDocument.Parse(await child.Content.ReadAsStringAsync());
        Assert.Equal(parentName, childJson.RootElement.GetProperty("data").GetProperty("parentCategory").GetString());
    }

    [Fact]
    public async Task Asset_category_list_returns_name_code_and_active()
    {
        await AuthorizeAsync();
        var name = $"Furniture {Guid.NewGuid():N}"[..24];
        var created = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name, code = $"FURN{Guid.NewGuid():N}"[..10], active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/asset-categories?search={Uri.EscapeDataString(name)}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(name, item.GetProperty("name").GetString());
        Assert.True(item.TryGetProperty("code", out _));
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.GetProperty("moreInformation").GetBoolean());
        Assert.False(item.TryGetProperty("alternateName", out _));

        var emptyCode = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name = $"Empty {Guid.NewGuid():N}"[..18], code = "", active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, emptyCode.StatusCode);
        var secondEmpty = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name = $"Empty2 {Guid.NewGuid():N}"[..18], code = "", active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, secondEmpty.StatusCode);
    }

    [Fact]
    public async Task Asset_category_history_records_logged_in_user()
    {
        await AuthorizeAsync();
        var name = $"Safety {Guid.NewGuid():N}"[..20];
        var created = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name, code = $"SAF{Guid.NewGuid():N}"[..10], active = true, moreInformation = false
        });
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var update = await _client.PutAsJsonAsync($"/api/asset-categories/{id}", new
        {
            name, code = createdJson.RootElement.GetProperty("data").GetProperty("code").GetString(),
            active = true, moreInformation = true, accountCode = "12345"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var history = await _client.GetAsync($"/api/asset-categories/{id}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var json = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var entries = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Record created"));
        Assert.Contains(entries, x => x.GetProperty("field").GetString() == "Account Code");
        Assert.All(entries, x =>
        {
            Assert.Equal("Administrator", x.GetProperty("by").GetString());
            Assert.Equal("Screen", x.GetProperty("source").GetString());
            Assert.True(x.TryGetProperty("when", out _));
        });
    }

    [Fact]
    public async Task Asset_category_more_information_false_ignores_optional_fields()
    {
        await AuthorizeAsync();
        var parentName = $"IT Equipment {Guid.NewGuid():N}"[..28];
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name = parentName, code = $"IT{Guid.NewGuid():N}"[..10],
            active = true, moreInformation = false
        })).StatusCode);

        var name = $"Network {Guid.NewGuid():N}"[..20];
        var created = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name, code = $"NET{Guid.NewGuid():N}"[..10], active = true,
            moreInformation = true, alternateName = "معدات الشبكة",
            parentCategory = parentName, accountCode = "1520"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal("معدات الشبكة", createdJson.RootElement.GetProperty("data").GetProperty("alternateName").GetString());
        Assert.True(createdJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());
        Assert.Equal(parentName, createdJson.RootElement.GetProperty("data").GetProperty("parentCategory").GetString());
        Assert.Equal("1520", createdJson.RootElement.GetProperty("data").GetProperty("accountCode").GetString());

        var ignored = await _client.PutAsJsonAsync($"/api/asset-categories/{id}", new
        {
            name, code = createdJson.RootElement.GetProperty("data").GetProperty("code").GetString(),
            active = true, moreInformation = false,
            alternateName = "ignored", parentCategory = "ignored", accountCode = "9999"
        });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignored.Content.ReadAsStringAsync());
        var updated = ignoredJson.RootElement.GetProperty("data");
        Assert.False(updated.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("معدات الشبكة", updated.GetProperty("alternateName").GetString());
        Assert.Equal(parentName, updated.GetProperty("parentCategory").GetString());
        Assert.Equal("1520", updated.GetProperty("accountCode").GetString());
    }

    [Fact]
    public async Task Manufacturer_list_returns_name_code_and_active()
    {
        await AuthorizeAsync();
        var name = $"Dell {Guid.NewGuid():N}"[..20];
        var code = $"DELL{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name, code, active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/manufacturers?search={Uri.EscapeDataString(name)}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(name, item.GetProperty("name").GetString());
        Assert.Equal(code, item.GetProperty("code").GetString());
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.GetProperty("moreInformation").GetBoolean());
        Assert.False(item.TryGetProperty("alternateName", out _));

        var emptyCode = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name = $"Empty {Guid.NewGuid():N}"[..18], code = "", active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, emptyCode.StatusCode);
        var secondEmpty = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name = $"Empty2 {Guid.NewGuid():N}"[..18], code = "", active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, secondEmpty.StatusCode);
    }

    [Fact]
    public async Task Manufacturer_detail_and_history_match_screens()
    {
        await AuthorizeAsync();
        var name = $"Cisco {Guid.NewGuid():N}"[..20];
        var code = $"CSCO{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name, code, active = true, moreInformation = true,
            alternateName = "", country = "United States",
            supportContact = "", website = "https://www.cisco.com"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var data = createdJson.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        Assert.True(data.GetProperty("active").GetBoolean());
        Assert.True(data.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("United States", data.GetProperty("country").GetString());
        Assert.Equal("https://www.cisco.com", data.GetProperty("website").GetString());
        Assert.Equal("Active → Inactive", data.GetProperty("lifecycle").GetString());

        var ignored = await _client.PutAsJsonAsync($"/api/manufacturers/{id}", new
        {
            name, code, active = true, moreInformation = false,
            country = "Egypt", website = "https://ignored.example"
        });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignored.Content.ReadAsStringAsync());
        Assert.Equal("United States", ignoredJson.RootElement.GetProperty("data").GetProperty("country").GetString());
        Assert.False(ignoredJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());

        var update = await _client.PutAsJsonAsync($"/api/manufacturers/{id}", new
        {
            name, code, active = true, moreInformation = true,
            country = "United States", supportContact = "support@cisco.com"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var history = await _client.GetAsync($"/api/manufacturers/{id}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var json = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var entries = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Record created"));
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Support Contact"));
        Assert.All(entries, x =>
        {
            Assert.Equal("Administrator", x.GetProperty("by").GetString());
            Assert.Equal("Screen", x.GetProperty("source").GetString());
            Assert.True(x.TryGetProperty("when", out _));
            Assert.True(x.TryGetProperty("change", out _));
            Assert.False(x.TryGetProperty("field", out _));
        });
    }

    [Fact]
    public async Task Asset_type_create_resolves_category_and_status_by_name_or_id()
    {
        await AuthorizeAsync();
        var categoryName = $"IT Equipment {Guid.NewGuid():N}"[..28];
        var categoryResponse = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name = categoryName, code = $"IT{Guid.NewGuid():N}"[..10], active = true, moreInformation = false
        });
        using var categoryJson = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var byName = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Laptop {Guid.NewGuid():N}"[..20],
            code = $"LAP{Guid.NewGuid():N}"[..10],
            assetCategory = categoryName,
            requiresSerialNumber = true,
            defaultStatus = "Working",
            active = true,
            moreInformation = true,
            alternateName = "حاسوب محمول",
            requiresRfidTag = true,
            requiresBarcode = false,
            numberingScheme = "LAP-#####",
            defaultDepreciationMethod = "Straight line",
            defaultUsefulLife = 36
        });
        Assert.Equal(HttpStatusCode.Created, byName.StatusCode);
        using var byNameJson = JsonDocument.Parse(await byName.Content.ReadAsStringAsync());
        var data = byNameJson.RootElement.GetProperty("data");
        Assert.Equal(categoryName, data.GetProperty("assetCategory").GetString());
        Assert.True(data.GetProperty("requiresSerialNumber").GetBoolean());
        Assert.Equal("Working", data.GetProperty("defaultStatus").GetString());
        Assert.True(data.GetProperty("active").GetBoolean());
        Assert.True(data.GetProperty("moreInformation").GetBoolean());
        Assert.True(data.GetProperty("requiresRfidTag").GetBoolean());
        Assert.False(data.GetProperty("requiresBarcode").GetBoolean());
        Assert.Equal("LAP-#####", data.GetProperty("numberingScheme").GetString());
        Assert.Equal(36, data.GetProperty("defaultUsefulLife").GetInt32());
        Assert.Equal("Active → Inactive", data.GetProperty("lifecycle").GetString());

        var byId = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Desktop {Guid.NewGuid():N}"[..20],
            code = $"DSK{Guid.NewGuid():N}"[..10],
            assetCategory = categoryId.ToString(),
            requiresSerialNumber = false,
            defaultStatus = "Working",
            active = true,
            moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, byId.StatusCode);
        using var byIdJson = JsonDocument.Parse(await byId.Content.ReadAsStringAsync());
        Assert.Equal(categoryName, byIdJson.RootElement.GetProperty("data").GetProperty("assetCategory").GetString());
        Assert.False(byIdJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());
        Assert.False(byIdJson.RootElement.GetProperty("data").GetProperty("requiresRfidTag").GetBoolean());

        var emptyCode = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Tablet {Guid.NewGuid():N}"[..20],
            code = "",
            active = true,
            moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, emptyCode.StatusCode);

        var secondEmpty = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Monitor {Guid.NewGuid():N}"[..20],
            code = "",
            active = true,
            moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, secondEmpty.StatusCode);

        var duplicateCode = $"DUP{Guid.NewGuid():N}"[..10];
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Phone {Guid.NewGuid():N}"[..20],
            code = duplicateCode,
            active = true,
            moreInformation = false
        })).StatusCode);
        var duplicate = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Phone copy {Guid.NewGuid():N}"[..24],
            code = duplicateCode,
            active = true,
            moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var missing = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = "Unknown Cat", code = $"UNK{Guid.NewGuid():N}"[..10],
            assetCategory = "Does Not Exist", requiresSerialNumber = false,
            active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Asset_type_list_returns_name_code_and_active()
    {
        await AuthorizeAsync();
        var code = $"SRV{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/asset-types", ValidAssetType(code));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/asset-types?search={code}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(code, item.GetProperty("code").GetString());
        Assert.True(item.TryGetProperty("name", out _));
        Assert.True(item.TryGetProperty("assetCategory", out _));
        Assert.False(item.GetProperty("requiresSerialNumber").GetBoolean());
        Assert.True(item.TryGetProperty("defaultStatus", out _));
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.GetProperty("moreInformation").GetBoolean());
        Assert.False(item.TryGetProperty("alternateName", out _));
    }

    [Fact]
    public async Task Asset_model_create_resolves_manufacturer_by_name()
    {
        await AuthorizeAsync();
        var manufacturerName = $"Dell {Guid.NewGuid():N}"[..20];
        var manufacturer = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name = manufacturerName, code = $"DL{Guid.NewGuid():N}"[..10],
            active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, manufacturer.StatusCode);

        var ignoredMoreInfo = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "Latitude 5540", manufacturer = manufacturerName, modelNumber = $"5540-{Guid.NewGuid():N}"[..16],
            active = true, moreInformation = false,
            alternateName = "string", assetType = "string", specifications = "string",
            expectedUsefulLife = 0, documentation = "string"
        });
        Assert.Equal(HttpStatusCode.Created, ignoredMoreInfo.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignoredMoreInfo.Content.ReadAsStringAsync());
        Assert.Equal(manufacturerName, ignoredJson.RootElement.GetProperty("data").GetProperty("manufacturer").GetString());
        Assert.True(ignoredJson.RootElement.GetProperty("data").GetProperty("active").GetBoolean());
        Assert.False(ignoredJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());
        Assert.True(ignoredJson.RootElement.GetProperty("data").GetProperty("assetType").ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);

        var withType = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "OptiPlex", manufacturer = manufacturerName, modelNumber = $"OPT-{Guid.NewGuid():N}"[..16],
            active = true, moreInformation = true,
            assetType = "General Asset", expectedUsefulLife = 48, specifications = "Business desktop"
        });
        Assert.Equal(HttpStatusCode.Created, withType.StatusCode);
        using var typeJson = JsonDocument.Parse(await withType.Content.ReadAsStringAsync());
        Assert.Equal("General Asset", typeJson.RootElement.GetProperty("data").GetProperty("assetType").GetString());
        Assert.Equal(48, typeJson.RootElement.GetProperty("data").GetProperty("expectedUsefulLife").GetInt32());
        Assert.True(typeJson.RootElement.GetProperty("data").GetProperty("active").GetBoolean());
        Assert.True(typeJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());

        var emptyNumber = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "Precision", manufacturer = manufacturerName, modelNumber = "",
            active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, emptyNumber.StatusCode);
        using var emptyJson = JsonDocument.Parse(await emptyNumber.Content.ReadAsStringAsync());
        Assert.True(emptyJson.RootElement.GetProperty("data").GetProperty("modelNumber").ValueKind
            is JsonValueKind.Null or JsonValueKind.Undefined
            || string.IsNullOrEmpty(emptyJson.RootElement.GetProperty("data").GetProperty("modelNumber").GetString()));
        Assert.True(emptyJson.RootElement.GetProperty("data").GetProperty("active").GetBoolean());

        var secondEmpty = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "XPS", manufacturer = manufacturerName, modelNumber = "",
            active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, secondEmpty.StatusCode);

        var omittedNumber = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "Vostro", manufacturer = manufacturerName,
            active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, omittedNumber.StatusCode);

        var filtered = await _client.GetAsync(
            $"/api/asset-models?assetType={Uri.EscapeDataString("General Asset")}&manufacturer={Uri.EscapeDataString(manufacturerName)}");
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
        using var filteredJson = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        var filteredItems = filteredJson.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToArray();
        Assert.Contains(filteredItems, x => x.GetProperty("name").GetString() == "OptiPlex");
        Assert.True(filteredItems.Single(x => x.GetProperty("name").GetString() == "OptiPlex")
            .GetProperty("moreInformation").GetBoolean());
        Assert.DoesNotContain(filteredItems, x => x.GetProperty("name").GetString() == "Latitude 5540");

        var missing = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "Unknown", manufacturer = "Does Not Exist", modelNumber = "X1",
            active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact(Skip = "Stops writing photograph files into the project.")]
    public async Task Invalid_image_content_is_rejected()
    {
        await AuthorizeAsync();
        using var lookup = JsonDocument.Parse(await _client.GetStringAsync("/api/assets/lookup"));
        var assetId = lookup.RootElement.GetProperty("data")[0].GetProperty("id").GetGuid();
        using var multipart = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes("this is not a png"));
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(bytes, "file", "fake.png");
        multipart.Add(new StringContent("false"), "isPrimary");
        multipart.Add(new StringContent("false"), "moreInformation");
        var response = await _client.PostAsync($"/api/asset-images?assetId={assetId}", multipart);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(Skip = "Stops writing photograph files into the project.")]
    public async Task Asset_image_list_and_detail_match_screens()
    {
        await AuthorizeAsync();
        using var lookup = JsonDocument.Parse(await _client.GetStringAsync("/api/assets/lookup"));
        var asset = lookup.RootElement.GetProperty("data")[0];
        var assetId = asset.GetProperty("id").GetGuid();
        var assetName = asset.GetProperty("name").GetString();

        using var multipart = Photograph("front.png", "Identification", true, "Laptop 04405 — front");
        var created = await _client.PostAsync($"/api/asset-images?assetId={assetId}", multipart);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var data = createdJson.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        Assert.Equal(assetName, data.GetProperty("asset").GetString());
        Assert.Equal("front.png", data.GetProperty("file").GetString());
        Assert.True(data.GetProperty("isPrimary").GetBoolean());
        Assert.True(data.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("Identification", data.GetProperty("purpose").GetString());
        Assert.Equal("Laptop 04405 — front", data.GetProperty("caption").GetString());
        Assert.Equal("Administrator", data.GetProperty("capturedBy").GetString());
        Assert.False(data.GetProperty("isLocked").GetBoolean());
        Assert.Equal($"/api/asset-images/{id}/content", data.GetProperty("contentUrl").GetString());
        Assert.Equal("Active → Superseded → Deleted (soft)", data.GetProperty("lifecycle").GetString());

        var content = await _client.GetAsync($"/api/asset-images/{id}/content");
        Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        Assert.Equal("image/png", content.Content.Headers.ContentType?.MediaType);

        using var nameplate = Photograph("plate.png", "Nameplate", false, "Serial nameplate");
        var second = await _client.PostAsync($"/api/asset-images?assetId={assetId}", nameplate);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var secondId = secondJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.False(secondJson.RootElement.GetProperty("data").GetProperty("isPrimary").GetBoolean());
        Assert.True(secondJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());

        var list = await _client.GetAsync($"/api/asset-images?asset={assetId}&purpose=Identification");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listed = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = listed.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(assetName, item.GetProperty("asset").GetString());
        Assert.Equal("front.png", item.GetProperty("file").GetString());
        Assert.True(item.GetProperty("isPrimary").GetBoolean());
        Assert.True(item.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("Identification", item.GetProperty("purpose").GetString());
        Assert.False(item.TryGetProperty("caption", out _));
        Assert.False(item.TryGetProperty("capturedBy", out _));
        Assert.False(item.TryGetProperty("contentUrl", out _));

        var byCreator = await _client.GetAsync("/api/asset-images?createdBy=Administrator");
        Assert.Equal(HttpStatusCode.OK, byCreator.StatusCode);
        using var byCreatorJson = JsonDocument.Parse(await byCreator.Content.ReadAsStringAsync());
        Assert.Contains(byCreatorJson.RootElement.GetProperty("data").GetProperty("items").EnumerateArray(),
            x => x.GetProperty("file").GetString() == "front.png");

        var makePrimary = await _client.PostAsync($"/api/asset-images/{secondId}/primary", new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, makePrimary.StatusCode);

        var ignored = await _client.PutAsJsonAsync($"/api/asset-images/{secondId}", new
        {
            asset = assetName, isPrimary = true, purpose = "Nameplate",
            moreInformation = false, caption = "ignored"
        });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignored.Content.ReadAsStringAsync());
        Assert.Equal("Serial nameplate", ignoredJson.RootElement.GetProperty("data").GetProperty("caption").GetString());
        Assert.False(ignoredJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());

        var history = await _client.GetAsync($"/api/asset-images/{id}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyJson = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var entries = historyJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Record created"));
        Assert.All(entries, x =>
        {
            Assert.Equal("Administrator", x.GetProperty("by").GetString());
            Assert.Equal("Screen", x.GetProperty("source").GetString());
            Assert.True(x.TryGetProperty("when", out _));
            Assert.False(x.TryGetProperty("field", out _));
        });
    }

    [Fact(Skip = "Stops writing photograph files into the project.")]
    public async Task Damage_evidence_photograph_is_locked()
    {
        await AuthorizeAsync();
        using var lookup = JsonDocument.Parse(await _client.GetStringAsync("/api/assets/lookup"));
        var assetId = lookup.RootElement.GetProperty("data")[0].GetProperty("id").GetGuid();
        using var multipart = Photograph("damage.png", "Damage evidence", false, "Crack on casing");
        var created = await _client.PostAsync($"/api/asset-images?assetId={assetId}", multipart);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.True(createdJson.RootElement.GetProperty("data").GetProperty("isLocked").GetBoolean());

        var update = await _client.PutAsJsonAsync($"/api/asset-images/{id}", new
        {
            asset = assetId.ToString(), isPrimary = false, purpose = "Damage evidence",
            moreInformation = true, caption = "changed"
        });
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);

        var delete = await _client.DeleteAsync($"/api/asset-images/{id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Combined_type_attribute_workflow_creates_definition_and_assignment()
    {
        await AuthorizeAsync();
        var typeResponse = await _client.PostAsJsonAsync(
            "/api/asset-types", ValidAssetType($"TYPE-{Guid.NewGuid():N}"));
        using var typeJson = JsonDocument.Parse(await typeResponse.Content.ReadAsStringAsync());
        var typeId = typeJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var code = $"field_{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/asset-type-attributes", new
        {
            assetType = typeId.ToString(), code, label = "Prototype Field",
            alternateName = "حقل تجريبي", dataType = "List",
            possibleValues = new[] { "One", "Two" }, @class = "Required",
            showInList = true, helpText = "Installed storage options."
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        Assert.Equal(code, data.GetProperty("code").GetString());
        Assert.Equal("Prototype Field", data.GetProperty("label").GetString());
        Assert.Equal("حقل تجريبي", data.GetProperty("alternateName").GetString());
        Assert.True(data.GetProperty("showInList").GetBoolean());
        Assert.Equal("List", data.GetProperty("dataType").GetString());
        Assert.Null(data.GetProperty("unit").GetString());
        Assert.Equal("Installed storage options.", data.GetProperty("helpText").GetString());
        Assert.Equal(["One", "Two"], data.GetProperty("possibleValues")
            .EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.Equal("Required", data.GetProperty("class").GetString());
        Assert.True(data.GetProperty("displayOrder").GetInt32() >= 1);
        Assert.True(data.GetProperty("active").GetBoolean());
        Assert.True(data.GetProperty("codeIsLocked").GetBoolean() is false);
        Assert.True(data.TryGetProperty("assetCount", out _));
        Assert.False(data.TryGetProperty("requirement", out _));

        var tooFew = await _client.PostAsJsonAsync($"/api/asset-types/{typeId}/attributes", new
        {
            code = $"few_{Guid.NewGuid():N}"[..12].ToLowerInvariant(),
            label = "Finish", dataType = "List", possibleValues = new[] { "One" },
            @class = "Optional"
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooFew.StatusCode);

        var listed = await _client.GetAsync($"/api/asset-types/{typeId}/attributes");
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        using var listedJson = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        var item = listedJson.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal("Prototype Field", item.GetProperty("label").GetString());
        Assert.Equal("حقل تجريبي", item.GetProperty("alternateName").GetString());
        Assert.Equal(code, item.GetProperty("code").GetString());
        Assert.Equal("List", item.GetProperty("dataType").GetString());
        Assert.Equal("Required", item.GetProperty("class").GetString());
        Assert.Null(item.GetProperty("unit").GetString());
        Assert.True(item.GetProperty("showInList").GetBoolean());
        Assert.Equal("Installed storage options.", item.GetProperty("helpText").GetString());
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.TryGetProperty("displayOrder", out _));
        Assert.False(item.TryGetProperty("codeIsLocked", out _));

        var grouped = await _client.GetAsync($"/api/asset-type-attributes?assetType={typeId}");
        Assert.Equal(HttpStatusCode.OK, grouped.StatusCode);
        using var groupedJson = JsonDocument.Parse(await grouped.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, groupedJson.RootElement.GetProperty("data").ValueKind);
        var group = groupedJson.RootElement.GetProperty("data").EnumerateArray()
            .Single(x => x.GetProperty("assetTypeId").GetGuid() == typeId);
        Assert.Equal(typeJson.RootElement.GetProperty("data").GetProperty("name").GetString(),
            group.GetProperty("assetType").GetString());
        Assert.True(group.GetProperty("extraFields").GetInt32() >= 1);
        Assert.True(group.TryGetProperty("assetCount", out _));
        var groupedItem = group.GetProperty("attributes")[0];
        Assert.Equal(code, groupedItem.GetProperty("code").GetString());
        Assert.Equal("Prototype Field", groupedItem.GetProperty("label").GetString());
        Assert.Equal("List", groupedItem.GetProperty("dataType").GetString());
        Assert.Equal("Required", groupedItem.GetProperty("class").GetString());

        var updated = await _client.PutAsJsonAsync($"/api/asset-type-attributes/{id}", new
        {
            assetType = typeId.ToString(), code, label = "Storage", alternateName = "حقل تجريبي",
            dataType = "List", possibleValues = new[] { "One", "Two", "Three" }, @class = "Recommended",
            displayOrder = -2, showInList = false, helpText = "Updated help."
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var updatedJson = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
        var saved = updatedJson.RootElement.GetProperty("data");
        Assert.Equal("Storage", saved.GetProperty("label").GetString());
        Assert.Equal("Recommended", saved.GetProperty("class").GetString());
        Assert.Equal(-2, saved.GetProperty("displayOrder").GetInt32());
        Assert.False(saved.GetProperty("showInList").GetBoolean());
        Assert.Null(saved.GetProperty("unit").GetString());
        Assert.Equal("Updated help.", saved.GetProperty("helpText").GetString());

        var types = await _client.GetAsync("/api/asset-type-attributes/types");
        Assert.Equal(HttpStatusCode.OK, types.StatusCode);
        using var typesJson = JsonDocument.Parse(await types.Content.ReadAsStringAsync());
        var typeOption = typesJson.RootElement.GetProperty("data").EnumerateArray()
            .Single(x => x.GetProperty("id").GetGuid() == typeId);
        Assert.True(typeOption.TryGetProperty("assetCount", out _));
        Assert.Equal(typeJson.RootElement.GetProperty("data").GetProperty("name").GetString(),
            typeOption.GetProperty("name").GetString());

        var numberOnly = await _client.PostAsJsonAsync("/api/asset-type-attributes", new
        {
            assetType = typeId.ToString(),
            code = $"ram_{Guid.NewGuid():N}"[..12].ToLowerInvariant(),
            label = "Memory", dataType = "Number", unit = "GB", @class = "Recommended",
            displayOrder = 0, helpText = "Installed RAM in gigabytes."
        });
        Assert.Equal(HttpStatusCode.Created, numberOnly.StatusCode);
        using var numberJson = JsonDocument.Parse(await numberOnly.Content.ReadAsStringAsync());
        var number = numberJson.RootElement.GetProperty("data");
        Assert.Equal("Number", number.GetProperty("dataType").GetString());
        Assert.Equal("GB", number.GetProperty("unit").GetString());
        Assert.True(number.TryGetProperty("possibleValues", out var ignoredValues) &&
            (ignoredValues.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined));

        var reference = await _client.PostAsJsonAsync("/api/asset-type-attributes", new
        {
            assetType = typeId.ToString(),
            code = $"ref_{Guid.NewGuid():N}"[..12].ToLowerInvariant(),
            label = "Owner", dataType = "Reference", @class = "Optional"
        });
        Assert.Equal(HttpStatusCode.BadRequest, reference.StatusCode);

        var exists = await _client.GetAsync(
            $"/api/asset-type-attributes/exists?code={code}&assetType={typeId}");
        using var existsJson = JsonDocument.Parse(await exists.Content.ReadAsStringAsync());
        Assert.True(existsJson.RootElement.GetProperty("data").GetBoolean());

        var retire = await _client.PostAsync($"/api/asset-type-attributes/{id}/retire", new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, retire.StatusCode);
        using var retiredJson = JsonDocument.Parse(await retire.Content.ReadAsStringAsync());
        Assert.False(retiredJson.RootElement.GetProperty("data").GetProperty("active").GetBoolean());

        var bringBack = await _client.PostAsync($"/api/asset-type-attributes/{id}/retire", new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, bringBack.StatusCode);
        using var restoredJson = JsonDocument.Parse(await bringBack.Content.ReadAsStringAsync());
        Assert.True(restoredJson.RootElement.GetProperty("data").GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Custom_attribute_definition_list_and_detail_match_screens()
    {
        await AuthorizeAsync();
        var typeName = $"Laptop {Guid.NewGuid():N}"[..20];
        var typeResponse = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = typeName, code = $"LAP{Guid.NewGuid():N}"[..10],
            requiresSerialNumber = false, active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);

        var code = $"ram_{Guid.NewGuid():N}"[..20].ToLowerInvariant();
        var created = await _client.PostAsJsonAsync("/api/custom-attribute-definitions", new
        {
            assetType = typeName, code, label = "Memory", dataType = "Number",
            effectiveClass = "Recommended", showInList = "Yes", active = "Yes",
            moreInformation = "Yes", alternateName = "الذاكرة", unit = "GB",
            helpText = "Installed RAM in gigabytes."
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var data = createdJson.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        Assert.Equal(typeName, data.GetProperty("assetType").GetString());
        Assert.Equal("Memory", data.GetProperty("label").GetString());
        Assert.Equal("Number", data.GetProperty("dataType").GetString());
        Assert.Equal("Recommended", data.GetProperty("effectiveClass").GetString());
        Assert.Equal("Yes", data.GetProperty("showInList").GetString());
        Assert.Equal("Yes", data.GetProperty("active").GetString());
        Assert.Equal("الذاكرة", data.GetProperty("alternateName").GetString());
        Assert.Equal("GB", data.GetProperty("unit").GetString());
        Assert.Equal("Draft → Active → Retired", data.GetProperty("lifecycle").GetString());
        Assert.True(data.GetProperty("displayOrder").GetInt32() >= 1);

        var list = await _client.GetAsync(
            $"/api/custom-attribute-definitions?search={code}&assetType={Uri.EscapeDataString(typeName)}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listed = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = listed.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(typeName, item.GetProperty("assetType").GetString());
        Assert.Equal(code, item.GetProperty("code").GetString());
        Assert.Equal("Memory", item.GetProperty("label").GetString());
        Assert.Equal("Number", item.GetProperty("dataType").GetString());
        Assert.Equal("Recommended", item.GetProperty("effectiveClass").GetString());
        Assert.True(item.TryGetProperty("displayOrder", out _));
        Assert.False(item.TryGetProperty("alternateName", out _));
        Assert.False(item.TryGetProperty("unit", out _));
        Assert.False(item.TryGetProperty("helpText", out _));
        Assert.False(item.TryGetProperty("active", out _));

        var ignored = await _client.PutAsJsonAsync($"/api/custom-attribute-definitions/{id}", new
        {
            assetType = typeName, code, label = "Memory", dataType = "Number",
            effectiveClass = "Required", showInList = "Yes", active = "Yes",
            moreInformation = "No", alternateName = "ignored", unit = "MB"
        });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignored.Content.ReadAsStringAsync());
        var updated = ignoredJson.RootElement.GetProperty("data");
        Assert.Equal("Required", updated.GetProperty("effectiveClass").GetString());
        Assert.Equal("الذاكرة", updated.GetProperty("alternateName").GetString());
        Assert.Equal("MB", updated.GetProperty("unit").GetString());

        var duplicate = await _client.PostAsJsonAsync("/api/custom-attribute-definitions", new
        {
            assetType = typeName, code, label = "Memory copy", dataType = "Number",
            effectiveClass = "Optional", active = "Yes", moreInformation = "No"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var yesNo = await _client.PostAsJsonAsync("/api/custom-attribute-definitions", new
        {
            assetType = typeName, code = $"psu_{Guid.NewGuid():N}"[..16].ToLowerInvariant(),
            label = "Redundant power", dataType = "Yes or no",
            effectiveClass = "Recommended", active = "Yes", moreInformation = "No"
        });
        Assert.Equal(HttpStatusCode.Created, yesNo.StatusCode);
        using var yesNoJson = JsonDocument.Parse(await yesNo.Content.ReadAsStringAsync());
        Assert.Equal("Yes or no", yesNoJson.RootElement.GetProperty("data").GetProperty("dataType").GetString());

        var history = await _client.GetAsync($"/api/custom-attribute-definitions/{id}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyJson = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var entries = historyJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Record created"));
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Effective Class"));
        Assert.All(entries, x =>
        {
            Assert.Equal("Administrator", x.GetProperty("by").GetString());
            Assert.Equal("Screen", x.GetProperty("source").GetString());
            Assert.False(x.TryGetProperty("field", out _));
        });
    }

    [Fact]
    public async Task Custom_attribute_definition_list_filters_by_asset_type_data_type_and_class()
    {
        await AuthorizeAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var laptop = $"Laptop {suffix}";
        var server = $"Server {suffix}";
        var laptopType = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = laptop, code = $"L{suffix}", requiresSerialNumber = false,
            active = true, moreInformation = false
        });
        var serverType = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = server, code = $"S{suffix}", requiresSerialNumber = false,
            active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, laptopType.StatusCode);
        Assert.Equal(HttpStatusCode.Created, serverType.StatusCode);
        using var laptopJson = JsonDocument.Parse(await laptopType.Content.ReadAsStringAsync());
        var laptopId = laptopJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/custom-attribute-definitions", new
        {
            assetType = laptop, code = $"ram_{suffix}", label = "Memory", dataType = "Number",
            effectiveClass = "Recommended", active = "Yes", moreInformation = "No"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/custom-attribute-definitions", new
        {
            assetType = server, code = $"disk_{suffix}", label = "Storage", dataType = "List",
            listValues = new[] { "SSD", "HDD" }, effectiveClass = "Required",
            active = "Yes", moreInformation = "Yes"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/custom-attribute-definitions", new
        {
            assetType = laptop, code = $"psu_{suffix}", label = "Redundant power", dataType = "Yes or no",
            effectiveClass = "Optional", active = "Yes", moreInformation = "No"
        })).StatusCode);

        async Task<string[]> CodesAsync(string query)
        {
            var response = await _client.GetAsync($"/api/custom-attribute-definitions?{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("data").GetProperty("items")
                .EnumerateArray().Select(x => x.GetProperty("code").GetString()!).ToArray();
        }

        var byName = await CodesAsync($"assetType={Uri.EscapeDataString(laptop)}");
        Assert.Contains($"ram_{suffix}", byName);
        Assert.Contains($"psu_{suffix}", byName);
        Assert.DoesNotContain($"disk_{suffix}", byName);

        var byPascalName = await CodesAsync($"AssetType={Uri.EscapeDataString(laptop)}");
        Assert.Equal(byName.Order(), byPascalName.Order());

        var byCode = await CodesAsync($"assetType=L{suffix}");
        Assert.Contains($"ram_{suffix}", byCode);
        Assert.DoesNotContain($"disk_{suffix}", byCode);

        var byId = await CodesAsync($"assetType={laptopId}");
        Assert.Contains($"ram_{suffix}", byId);
        Assert.DoesNotContain($"disk_{suffix}", byId);

        var byTypeId = await CodesAsync($"assetTypeId={laptopId}");
        Assert.Contains($"ram_{suffix}", byTypeId);
        Assert.DoesNotContain($"disk_{suffix}", byTypeId);

        var numbers = await CodesAsync("dataType=Number");
        Assert.Contains($"ram_{suffix}", numbers);
        Assert.DoesNotContain($"disk_{suffix}", numbers);
        Assert.DoesNotContain($"psu_{suffix}", numbers);

        var lists = await CodesAsync("DataType=List");
        Assert.Contains($"disk_{suffix}", lists);
        Assert.DoesNotContain($"ram_{suffix}", lists);

        var yesNo = await CodesAsync($"dataType={Uri.EscapeDataString("Yes or no")}");
        Assert.Contains($"psu_{suffix}", yesNo);
        Assert.DoesNotContain($"ram_{suffix}", yesNo);

        var yesNoAlias = await CodesAsync("dataType=YesNo");
        Assert.Contains($"psu_{suffix}", yesNoAlias);

        var required = await CodesAsync("effectiveClass=Required");
        Assert.Contains($"disk_{suffix}", required);
        Assert.DoesNotContain($"ram_{suffix}", required);

        var recommended = await CodesAsync("EffectiveClass=recommended");
        Assert.Contains($"ram_{suffix}", recommended);
        Assert.DoesNotContain($"disk_{suffix}", recommended);

        var combined = await CodesAsync(
            $"assetType={Uri.EscapeDataString(laptop)}&dataType=Number&effectiveClass=Recommended");
        Assert.Contains($"ram_{suffix}", combined);
        Assert.DoesNotContain($"psu_{suffix}", combined);
        Assert.DoesNotContain($"disk_{suffix}", combined);
    }

    [Fact]
    public async Task Rfid_tag_list_matches_prototype_columns()
    {
        await AuthorizeAsync();
        var tag = $"E280:TEST:{Guid.NewGuid():N}"[..20];
        var created = await _client.PostAsJsonAsync("/api/rfid-tags", new
        {
            tagIdentifier = tag, tagType = "Passive UHF", status = "Unassigned", moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/rfid-tags?search={Uri.EscapeDataString(tag)}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(tag, item.GetProperty("tagIdentifier").GetString());
        Assert.Equal("Passive UHF", item.GetProperty("tagType").GetString());
        Assert.Equal("Unassigned", item.GetProperty("status").GetString());
        Assert.False(item.GetProperty("moreInformation").GetBoolean());
        Assert.True(item.TryGetProperty("encodingStandard", out _));
        Assert.False(item.TryGetProperty("asset", out _));
        Assert.False(item.TryGetProperty("encodedAt", out _));
        Assert.False(item.TryGetProperty("lifecycle", out _));
    }

    [Fact]
    public async Task Rfid_tag_detail_matches_new_rfid_tag_screen()
    {
        await AuthorizeAsync();
        var tag = $"E280:TEST:{Guid.NewGuid():N}"[..20];
        var created = await _client.PostAsJsonAsync("/api/rfid-tags", new
        {
            tagIdentifier = tag, tagType = "Active", encodingStandard = "GS1 SGTIN", status = "Unassigned",
            moreInformation = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.True(createdJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());

        var get = await _client.GetAsync($"/api/rfid-tags/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(tag, data.GetProperty("tagIdentifier").GetString());
        Assert.Equal("Active", data.GetProperty("tagType").GetString());
        Assert.Equal("GS1 SGTIN", data.GetProperty("encodingStandard").GetString());
        Assert.Equal("Unassigned", data.GetProperty("status").GetString());
        Assert.True(data.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("Unassigned → Assigned → Damaged | Replaced → Retired",
            data.GetProperty("lifecycle").GetString());
        Assert.True(data.TryGetProperty("asset", out _));
        Assert.True(data.TryGetProperty("encodedAt", out _));
        Assert.True(data.TryGetProperty("encodedBy", out _));
        Assert.True(data.TryGetProperty("replacedBy", out _));
        Assert.True(data.TryGetProperty("retiredAt", out _));
    }

    [Fact]
    public async Task Rfid_tag_can_be_assigned_replaced_and_listed_in_stock()
    {
        await AuthorizeAsync();
        var stock = $"E280:STK:{Guid.NewGuid():N}"[..20];
        var replacement = $"E280:REP:{Guid.NewGuid():N}"[..20];
        var first = await _client.PostAsJsonAsync("/api/rfid-tags", new
        {
            tagIdentifier = stock, tagType = "Passive HF", status = "Unassigned", moreInformation = false
        });
        var second = await _client.PostAsJsonAsync("/api/rfid-tags", new
        {
            tagIdentifier = replacement, tagType = "Passive HF", status = "Unassigned", moreInformation = false
        });
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var firstId = firstJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var secondId = secondJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var waiting = await _client.GetAsync("/api/rfid-tags/assets-waiting?search=Demo");
        Assert.Equal(HttpStatusCode.OK, waiting.StatusCode);
        using var waitingJson = JsonDocument.Parse(await waiting.Content.ReadAsStringAsync());
        var asset = waitingJson.RootElement.GetProperty("data").GetProperty("items")[0];
        var assetId = asset.GetProperty("id").GetGuid();
        Assert.Equal("Demo Asset", asset.GetProperty("name").GetString());
        Assert.True(asset.TryGetProperty("assetType", out _));

        var assign = await _client.PostAsJsonAsync($"/api/rfid-tags/{firstId}/assign", new { assetId });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        using var assigned = JsonDocument.Parse(await assign.Content.ReadAsStringAsync());
        Assert.Equal("Assigned", assigned.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal("Demo Asset", assigned.RootElement.GetProperty("data").GetProperty("asset").GetString());

        var replace = await _client.PostAsJsonAsync($"/api/rfid-tags/{firstId}/replace", new { replacementId = secondId });
        Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
        using var replaced = JsonDocument.Parse(await replace.Content.ReadAsStringAsync());
        Assert.Equal("Assigned", replaced.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(replacement, replaced.RootElement.GetProperty("data").GetProperty("tagIdentifier").GetString());

        var old = await _client.GetAsync($"/api/rfid-tags/{firstId}");
        using var oldJson = JsonDocument.Parse(await old.Content.ReadAsStringAsync());
        Assert.Equal("Replaced", oldJson.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(replacement, oldJson.RootElement.GetProperty("data").GetProperty("replacedBy").GetString());

        var history = await _client.GetAsync($"/api/rfid-tags/{firstId}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyJson = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        Assert.True(historyJson.RootElement.GetProperty("data").GetArrayLength() > 0);
        Assert.Equal("Screen", historyJson.RootElement.GetProperty("data")[0].GetProperty("source").GetString());

        var unassignedStock = await _client.GetAsync("/api/rfid-tags?status=Unassigned");
        using var stockJson = JsonDocument.Parse(await unassignedStock.Content.ReadAsStringAsync());
        foreach (var item in stockJson.RootElement.GetProperty("data").GetProperty("items").EnumerateArray())
            Assert.Equal("Unassigned", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Barcode_list_matches_prototype_columns()
    {
        await AuthorizeAsync();
        var value = $"BC-{Guid.NewGuid():N}"[..12];
        var created = await _client.PostAsJsonAsync("/api/barcodes", new
        {
            value, symbology = "Code128", status = "Unassigned", moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/barcodes?search={Uri.EscapeDataString(value)}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(value, item.GetProperty("value").GetString());
        Assert.Equal("Code128", item.GetProperty("symbology").GetString());
        Assert.Equal("Unassigned", item.GetProperty("status").GetString());
        Assert.False(item.GetProperty("moreInformation").GetBoolean());
        Assert.False(item.TryGetProperty("subjectReference", out _));
        Assert.False(item.TryGetProperty("printedAt", out _));
        Assert.False(item.TryGetProperty("lifecycle", out _));
    }

    [Fact]
    public async Task Barcode_detail_matches_new_barcode_screen()
    {
        await AuthorizeAsync();
        var value = $"BC-{Guid.NewGuid():N}"[..12];
        var created = await _client.PostAsJsonAsync("/api/barcodes", new
        {
            value, symbology = "DataMatrix", status = "Unassigned", moreInformation = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var get = await _client.GetAsync($"/api/barcodes/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(value, data.GetProperty("value").GetString());
        Assert.Equal("DataMatrix", data.GetProperty("symbology").GetString());
        Assert.Equal("Unassigned", data.GetProperty("status").GetString());
        Assert.True(data.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("Unassigned → Assigned → Replaced → Retired", data.GetProperty("lifecycle").GetString());
        Assert.True(data.TryGetProperty("subjectReference", out _));
        Assert.True(data.TryGetProperty("printedAt", out _));
        Assert.True(data.TryGetProperty("replacedBy", out _));
        Assert.False(data.TryGetProperty("encodedBy", out _));
    }

    [Fact]
    public async Task Barcode_can_be_assigned_replaced_and_listed_in_stock()
    {
        await AuthorizeAsync();
        var stock = $"BC-{Guid.NewGuid():N}"[..12];
        var replacement = $"BC-{Guid.NewGuid():N}"[..12];
        var first = await _client.PostAsJsonAsync("/api/barcodes", new
        {
            value = stock, symbology = "QR", status = "Unassigned", moreInformation = false
        });
        var second = await _client.PostAsJsonAsync("/api/barcodes", new
        {
            value = replacement, symbology = "QR", status = "Unassigned", moreInformation = false
        });
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var firstId = firstJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var secondId = secondJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var waiting = await _client.GetAsync("/api/barcodes/assets-waiting?search=Demo");
        Assert.Equal(HttpStatusCode.OK, waiting.StatusCode);
        using var waitingJson = JsonDocument.Parse(await waiting.Content.ReadAsStringAsync());
        var asset = waitingJson.RootElement.GetProperty("data").GetProperty("items")[0];
        var assetId = asset.GetProperty("id").GetGuid();
        Assert.Equal("Demo Asset", asset.GetProperty("name").GetString());

        var assign = await _client.PostAsJsonAsync($"/api/barcodes/{firstId}/assign", new { assetId });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        using var assigned = JsonDocument.Parse(await assign.Content.ReadAsStringAsync());
        Assert.Equal("Assigned", assigned.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal("Demo Asset", assigned.RootElement.GetProperty("data").GetProperty("subjectReference").GetString());

        var replace = await _client.PostAsJsonAsync($"/api/barcodes/{firstId}/replace", new { replacementId = secondId });
        Assert.Equal(HttpStatusCode.OK, replace.StatusCode);

        var old = await _client.GetAsync($"/api/barcodes/{firstId}");
        using var oldJson = JsonDocument.Parse(await old.Content.ReadAsStringAsync());
        Assert.Equal("Replaced", oldJson.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(replacement, oldJson.RootElement.GetProperty("data").GetProperty("replacedBy").GetString());

        var history = await _client.GetAsync($"/api/barcodes/{firstId}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyJson = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        Assert.True(historyJson.RootElement.GetProperty("data").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Asset_status_list_returns_screen_columns()
    {
        await AuthorizeAsync();
        var code = $"ST{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/asset-statuses", ValidAssetStatus(code));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/asset-statuses?search={code}&statusCategory=Working");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(code, item.GetProperty("code").GetString());
        Assert.Equal("Working", item.GetProperty("statusCategory").GetString());
        Assert.Equal("#16a34a", item.GetProperty("color").GetString());
        Assert.True(item.GetProperty("isOperational").GetBoolean());
        Assert.False(item.GetProperty("isTerminal").GetBoolean());
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.GetProperty("moreInformation").GetBoolean());
        Assert.False(item.TryGetProperty("alternateName", out _));
        Assert.False(item.TryGetProperty("blocksMovement", out _));
        Assert.False(item.TryGetProperty("sortOrder", out _));
    }

    [Fact]
    public async Task Asset_status_detail_history_and_transitions_match_screens()
    {
        await AuthorizeAsync();
        var code = $"ST{Guid.NewGuid():N}"[..10];
        var name = $"Hold {Guid.NewGuid():N}"[..18];
        var created = await _client.PostAsJsonAsync("/api/asset-statuses", new
        {
            code, name, statusCategory = "In Maintenance", color = "#f59e0b",
            isOperational = false, isTerminal = false, blocksMovement = true,
            active = true, moreInformation = true, alternateName = "موقوف"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var data = createdJson.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        Assert.Equal("In Maintenance", data.GetProperty("statusCategory").GetString());
        Assert.Equal("#f59e0b", data.GetProperty("color").GetString());
        Assert.False(data.GetProperty("isOperational").GetBoolean());
        Assert.False(data.GetProperty("isTerminal").GetBoolean());
        Assert.True(data.GetProperty("blocksMovement").GetBoolean());
        Assert.True(data.GetProperty("active").GetBoolean());
        Assert.True(data.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("موقوف", data.GetProperty("alternateName").GetString());
        Assert.True(data.GetProperty("sortOrder").GetInt32() > 0);
        Assert.Equal("Active → Inactive. Never deleted while referenced by history.",
            data.GetProperty("lifecycle").GetString());

        var ignored = await _client.PutAsJsonAsync($"/api/asset-statuses/{id}", new
        {
            code, name, statusCategory = "In Maintenance", color = "#f59e0b",
            isOperational = false, isTerminal = false, blocksMovement = true,
            active = true, moreInformation = false, alternateName = "ignored"
        });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignored.Content.ReadAsStringAsync());
        Assert.Equal("موقوف", ignoredJson.RootElement.GetProperty("data").GetProperty("alternateName").GetString());
        Assert.False(ignoredJson.RootElement.GetProperty("data").GetProperty("moreInformation").GetBoolean());

        var lookup = await _client.GetAsync("/api/asset-statuses/lookup?search=Working");
        using var lookupJson = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());
        var workingId = lookupJson.RootElement.GetProperty("data").EnumerateArray()
            .First(x => x.GetProperty("code").GetString() == "WRK").GetProperty("id").GetGuid();

        var transitions = await _client.PutAsJsonAsync($"/api/asset-statuses/{id}/allowed-transitions",
            new { allowedToStatusIds = new[] { workingId } });
        Assert.Equal(HttpStatusCode.OK, transitions.StatusCode);
        using var transitionJson = JsonDocument.Parse(await transitions.Content.ReadAsStringAsync());
        Assert.Equal("WRK", transitionJson.RootElement.GetProperty("data")[0].GetProperty("code").GetString());

        var self = await _client.PutAsJsonAsync($"/api/asset-statuses/{id}/allowed-transitions",
            new { allowedToStatusIds = new[] { id } });
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);

        var disposedLookup = await _client.GetAsync("/api/asset-statuses/lookup?search=Disposed");
        using var disposedJson = JsonDocument.Parse(await disposedLookup.Content.ReadAsStringAsync());
        var disposedId = disposedJson.RootElement.GetProperty("data").EnumerateArray()
            .First(x => x.GetProperty("code").GetString() == "DSP").GetProperty("id").GetGuid();
        var terminal = await _client.PutAsJsonAsync($"/api/asset-statuses/{disposedId}/allowed-transitions",
            new { allowedToStatusIds = new[] { workingId } });
        Assert.Equal(HttpStatusCode.Conflict, terminal.StatusCode);

        var history = await _client.GetAsync($"/api/asset-statuses/{id}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var json = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var entries = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Record created"));
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Allowed transitions"));
        Assert.All(entries, x =>
        {
            Assert.Equal("Administrator", x.GetProperty("by").GetString());
            Assert.Equal("Screen", x.GetProperty("source").GetString());
            Assert.True(x.TryGetProperty("when", out _));
            Assert.True(x.TryGetProperty("change", out _));
            Assert.False(x.TryGetProperty("field", out _));
        });
    }

    [Fact]
    public async Task Supplier_list_returns_screen_columns()
    {
        await AuthorizeAsync();
        var code = $"SUP{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/suppliers", new
        {
            code, name = "Delta Technology Distribution",
            supplierKind = "Vendor", contactPerson = "H. Farouk",
            telephone = "+20 2 2735 4410", email = "sales@deltatech.example",
            country = "Egypt", active = true, moreInformation = false
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/suppliers?search={code}&supplierKind=Vendor");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(code, item.GetProperty("code").GetString());
        Assert.Equal("Delta Technology Distribution", item.GetProperty("name").GetString());
        Assert.Equal("Vendor", item.GetProperty("supplierKind").GetString());
        Assert.Equal("H. Farouk", item.GetProperty("contactPerson").GetString());
        Assert.Equal("+20 2 2735 4410", item.GetProperty("telephone").GetString());
        Assert.Equal("sales@deltatech.example", item.GetProperty("email").GetString());
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.GetProperty("moreInformation").GetBoolean());
        Assert.False(item.TryGetProperty("alternateName", out _));
        Assert.False(item.TryGetProperty("taxRegistration", out _));
        Assert.False(item.TryGetProperty("address", out _));
        Assert.False(item.TryGetProperty("country", out _));
        Assert.False(item.TryGetProperty("rating", out _));
    }

    [Fact]
    public async Task Supplier_detail_and_history_match_screens()
    {
        await AuthorizeAsync();
        var code = $"SUP{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/suppliers", new
        {
            code, name = "Nile Office Systems",
            supplierKind = "Both", contactPerson = "M. Adly",
            telephone = "+20 2 2419 7782", email = "info@nileoffice.example",
            country = "Egypt", active = true, moreInformation = true,
            alternateName = "النيل لأنظمة المكاتب", taxRegistration = $"TAX{Guid.NewGuid():N}"[..12],
            paymentTerms = "45 days net", rating = "Approved", externalIdentifier = $"ODOO-{Guid.NewGuid():N}"[..12]
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var data = createdJson.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        Assert.Equal("Both", data.GetProperty("supplierKind").GetString());
        Assert.Equal("M. Adly", data.GetProperty("contactPerson").GetString());
        Assert.True(data.GetProperty("active").GetBoolean());
        Assert.True(data.GetProperty("moreInformation").GetBoolean());
        Assert.Equal("النيل لأنظمة المكاتب", data.GetProperty("alternateName").GetString());
        Assert.Equal("Approved", data.GetProperty("rating").GetString());
        Assert.Equal("Draft → Active → Under review → Blocked → Archived",
            data.GetProperty("lifecycle").GetString());

        var ignored = await _client.PutAsJsonAsync($"/api/suppliers/{id}", new
        {
            code, name = "Nile Office Systems",
            supplierKind = "Both", contactPerson = "M. Adly",
            telephone = "+20 2 2419 7782", email = "info@nileoffice.example",
            country = "United Arab Emirates", active = true, moreInformation = false,
            alternateName = "ignored", rating = "Blocked"
        });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignored.Content.ReadAsStringAsync());
        var updated = ignoredJson.RootElement.GetProperty("data");
        Assert.Equal("United Arab Emirates", updated.GetProperty("country").GetString());
        Assert.Equal("النيل لأنظمة المكاتب", updated.GetProperty("alternateName").GetString());
        Assert.Equal("Approved", updated.GetProperty("rating").GetString());
        Assert.False(updated.GetProperty("moreInformation").GetBoolean());

        var history = await _client.GetAsync($"/api/suppliers/{id}/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var json = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var entries = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Record created"));
        Assert.Contains(entries, x => x.GetProperty("change").GetString()!.Contains("Country"));
        Assert.All(entries, x =>
        {
            Assert.Equal("Administrator", x.GetProperty("by").GetString());
            Assert.Equal("Screen", x.GetProperty("source").GetString());
            Assert.True(x.TryGetProperty("when", out _));
            Assert.True(x.TryGetProperty("change", out _));
            Assert.False(x.TryGetProperty("field", out _));
        });
    }

    private async Task AuthorizeAsync()
    {
        if (string.IsNullOrWhiteSpace(factory.AccessToken))
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "Admin@123" });
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            factory.AccessToken = json.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.AccessToken);
    }

    private static object ValidMaster(string code) => new { code, name = $"Name {code}", description = "Test record" };

    private static object ValidManufacturer(string code) => new
    {
        name = $"Name {code}", code, active = true, moreInformation = false
    };

    private static object ValidAssetType(string code) => new
    {
        name = $"Name {code}", code, requiresSerialNumber = false,
        active = true, moreInformation = false
    };

    private static object ValidAssetStatus(string code) => new
    {
        code, name = $"Name {code}", statusCategory = "Working", color = "#16a34a",
        isOperational = true, isTerminal = false, blocksMovement = false,
        active = true, moreInformation = false
    };

    private static MultipartFormDataContent Photograph(
        string fileName, string purpose, bool isPrimary, string caption, bool moreInformation = true)
    {
        var multipart = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(bytes, "file", fileName);
        multipart.Add(new StringContent(purpose), "purpose");
        multipart.Add(new StringContent(isPrimary ? "true" : "false"), "isPrimary");
        multipart.Add(new StringContent(moreInformation ? "true" : "false"), "moreInformation");
        multipart.Add(new StringContent(caption), "caption");
        return multipart;
    }
}
