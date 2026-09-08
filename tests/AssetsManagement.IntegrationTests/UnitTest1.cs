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

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "AssetsManagement.Tests",
                ["Jwt:Audience"] = "AssetsManagement.Tests.Client",
                ["Jwt:SigningKey"] = "integration-test-signing-key-must-be-at-least-thirty-two-bytes",
                ["Jwt:ExpiryMinutes"] = "10",
                ["Database:SeedOnStartup"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "unused"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AssetsDbContext>>();
            services.RemoveAll<AssetsDbContext>();
            services.AddSingleton(_connection);
            services.AddDbContext<AssetsDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public string? AccessToken { get; set; }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _ = CreateClient();
        await DataSeeder.SeedAsync(Services);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

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
            name = parentName, code = $"IT{Guid.NewGuid():N}"[..10], active = true
        });
        Assert.Equal(HttpStatusCode.Created, parentResponse.StatusCode);

        var child = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name = $"Network Equipment {Guid.NewGuid():N}"[..32],
            code = $"NET{Guid.NewGuid():N}"[..10],
            active = true,
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
            name, code = $"FURN{Guid.NewGuid():N}"[..10], active = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/asset-categories?search={Uri.EscapeDataString(name)}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(name, item.GetProperty("name").GetString());
        Assert.True(item.TryGetProperty("code", out _));
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.TryGetProperty("alternateName", out _));
        Assert.False(item.TryGetProperty("accountCode", out _));
    }

    [Fact]
    public async Task Asset_category_history_records_logged_in_user()
    {
        await AuthorizeAsync();
        var name = $"Safety {Guid.NewGuid():N}"[..20];
        var created = await _client.PostAsJsonAsync("/api/asset-categories", new
        {
            name, code = $"SAF{Guid.NewGuid():N}"[..10], active = true
        });
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var update = await _client.PutAsJsonAsync($"/api/asset-categories/{id}", new
        {
            name, code = createdJson.RootElement.GetProperty("data").GetProperty("code").GetString(),
            active = true, accountCode = "12345"
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
    public async Task Manufacturer_list_returns_name_code_and_active()
    {
        await AuthorizeAsync();
        var name = $"Dell {Guid.NewGuid():N}"[..20];
        var code = $"DELL{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name, code, active = "Yes", moreInformation = "No"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await _client.GetAsync($"/api/manufacturers?search={Uri.EscapeDataString(name)}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(name, item.GetProperty("name").GetString());
        Assert.Equal(code, item.GetProperty("code").GetString());
        Assert.True(item.GetProperty("active").GetBoolean());
        Assert.False(item.TryGetProperty("alternateName", out _));
        Assert.False(item.TryGetProperty("country", out _));
        Assert.False(item.TryGetProperty("website", out _));
    }

    [Fact]
    public async Task Manufacturer_detail_and_history_match_screens()
    {
        await AuthorizeAsync();
        var name = $"Cisco {Guid.NewGuid():N}"[..20];
        var code = $"CSCO{Guid.NewGuid():N}"[..10];
        var created = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name, code, active = "Yes", moreInformation = "Yes",
            alternateName = "", country = "United States",
            supportContact = "", website = "https://www.cisco.com"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var data = createdJson.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        Assert.Equal("Yes", data.GetProperty("active").GetString());
        Assert.Equal("United States", data.GetProperty("country").GetString());
        Assert.Equal("https://www.cisco.com", data.GetProperty("website").GetString());
        Assert.Equal("Active → Inactive", data.GetProperty("lifecycle").GetString());

        var ignored = await _client.PutAsJsonAsync($"/api/manufacturers/{id}", new
        {
            name, code, active = "Yes", moreInformation = "No",
            country = "Egypt", website = "https://ignored.example"
        });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignored.Content.ReadAsStringAsync());
        Assert.Equal("United States", ignoredJson.RootElement.GetProperty("data").GetProperty("country").GetString());

        var update = await _client.PutAsJsonAsync($"/api/manufacturers/{id}", new
        {
            name, code, active = "Yes", moreInformation = "Yes",
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
            name = categoryName, code = $"IT{Guid.NewGuid():N}"[..10], active = true
        });
        using var categoryJson = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var byName = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Laptop {Guid.NewGuid():N}"[..20],
            code = $"LAP{Guid.NewGuid():N}"[..10],
            assetCategory = categoryName,
            requiresSerialNumber = "Yes",
            defaultStatus = "Working",
            active = "Yes",
            moreInformation = "Yes",
            alternateName = "حاسوب محمول",
            requiresRfidTag = "Yes",
            requiresBarcode = "No",
            numberingScheme = "LAP-#####",
            defaultDepreciationMethod = "Straight line",
            defaultUsefulLife = 36
        });
        Assert.Equal(HttpStatusCode.Created, byName.StatusCode);
        using var byNameJson = JsonDocument.Parse(await byName.Content.ReadAsStringAsync());
        var data = byNameJson.RootElement.GetProperty("data");
        Assert.Equal(categoryName, data.GetProperty("assetCategory").GetString());
        Assert.Equal("Yes", data.GetProperty("requiresSerialNumber").GetString());
        Assert.Equal("Working", data.GetProperty("defaultStatus").GetString());
        Assert.Equal("Yes", data.GetProperty("active").GetString());
        Assert.Equal("Yes", data.GetProperty("requiresRfidTag").GetString());
        Assert.Equal("LAP-#####", data.GetProperty("numberingScheme").GetString());
        Assert.Equal(36, data.GetProperty("defaultUsefulLife").GetInt32());
        Assert.Equal("Active → Inactive", data.GetProperty("lifecycle").GetString());

        var byId = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = $"Desktop {Guid.NewGuid():N}"[..20],
            code = $"DSK{Guid.NewGuid():N}"[..10],
            assetCategory = categoryId.ToString(),
            requiresSerialNumber = "No",
            defaultStatus = "Working",
            active = "Yes",
            moreInformation = "No"
        });
        Assert.Equal(HttpStatusCode.Created, byId.StatusCode);
        using var byIdJson = JsonDocument.Parse(await byId.Content.ReadAsStringAsync());
        Assert.Equal(categoryName, byIdJson.RootElement.GetProperty("data").GetProperty("assetCategory").GetString());
        Assert.Equal("No", byIdJson.RootElement.GetProperty("data").GetProperty("requiresRfidTag").GetString());

        var missing = await _client.PostAsJsonAsync("/api/asset-types", new
        {
            name = "Unknown Cat", code = $"UNK{Guid.NewGuid():N}"[..10],
            assetCategory = "Does Not Exist", requiresSerialNumber = "No",
            active = "Yes", moreInformation = "No"
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
        Assert.False(item.TryGetProperty("alternateName", out _));
        Assert.False(item.TryGetProperty("numberingScheme", out _));
    }

    [Fact]
    public async Task Asset_model_create_resolves_manufacturer_by_name()
    {
        await AuthorizeAsync();
        var manufacturerName = $"Dell {Guid.NewGuid():N}"[..20];
        var manufacturer = await _client.PostAsJsonAsync("/api/manufacturers", new
        {
            name = manufacturerName, code = $"DL{Guid.NewGuid():N}"[..10],
            active = "Yes", moreInformation = "No"
        });
        Assert.Equal(HttpStatusCode.Created, manufacturer.StatusCode);

        var ignoredMoreInfo = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "Latitude 5540", manufacturer = manufacturerName, modelNumber = $"5540-{Guid.NewGuid():N}"[..16],
            active = "Yes", moreInformation = "No",
            alternateName = "string", assetType = "string", specifications = "string",
            expectedUsefulLife = 0, documentation = "string"
        });
        Assert.Equal(HttpStatusCode.Created, ignoredMoreInfo.StatusCode);
        using var ignoredJson = JsonDocument.Parse(await ignoredMoreInfo.Content.ReadAsStringAsync());
        Assert.Equal(manufacturerName, ignoredJson.RootElement.GetProperty("data").GetProperty("manufacturer").GetString());
        Assert.Equal("Yes", ignoredJson.RootElement.GetProperty("data").GetProperty("active").GetString());
        Assert.True(ignoredJson.RootElement.GetProperty("data").GetProperty("assetType").ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);

        var withType = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "OptiPlex", manufacturer = manufacturerName, modelNumber = $"OPT-{Guid.NewGuid():N}"[..16],
            active = "Yes", moreInformation = "Yes",
            assetType = "General Asset", expectedUsefulLife = 48, specifications = "Business desktop"
        });
        Assert.Equal(HttpStatusCode.Created, withType.StatusCode);
        using var typeJson = JsonDocument.Parse(await withType.Content.ReadAsStringAsync());
        Assert.Equal("General Asset", typeJson.RootElement.GetProperty("data").GetProperty("assetType").GetString());
        Assert.Equal(48, typeJson.RootElement.GetProperty("data").GetProperty("expectedUsefulLife").GetInt32());

        var filtered = await _client.GetAsync(
            $"/api/asset-models?assetType={Uri.EscapeDataString("General Asset")}&manufacturer={Uri.EscapeDataString(manufacturerName)}");
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
        using var filteredJson = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        var filteredItems = filteredJson.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToArray();
        Assert.Contains(filteredItems, x => x.GetProperty("name").GetString() == "OptiPlex");
        Assert.DoesNotContain(filteredItems, x => x.GetProperty("name").GetString() == "Latitude 5540");

        var missing = await _client.PostAsJsonAsync("/api/asset-models", new
        {
            name = "Unknown", manufacturer = "Does Not Exist", modelNumber = "X1",
            active = "Yes", moreInformation = "No"
        });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Invalid_image_content_is_rejected()
    {
        await AuthorizeAsync();
        using var lookup = JsonDocument.Parse(await _client.GetStringAsync("/api/assets/lookup"));
        var assetId = lookup.RootElement.GetProperty("data")[0].GetProperty("id").GetGuid();
        using var multipart = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes("this is not a png"));
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(bytes, "file", "fake.png");
        var response = await _client.PostAsync($"/api/asset-images?assetId={assetId}", multipart);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
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

        var response = await _client.PostAsJsonAsync($"/api/asset-types/{typeId}/attributes", new
        {
            code, label = "Prototype Field", dataType = "List",
            listValues = new[] { "One", "Two" }, requirement = "Required",
            displayOrder = 1, showInList = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("data").GetProperty("code").GetString());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("showInList").GetBoolean());
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
        name = $"Name {code}", code, active = "Yes", moreInformation = "No"
    };

    private static object ValidAssetType(string code) => new
    {
        name = $"Name {code}", code, requiresSerialNumber = "No",
        active = "Yes", moreInformation = "No"
    };
}