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
