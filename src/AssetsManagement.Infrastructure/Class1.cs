using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AssetsManagement.Infrastructure;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}

public sealed class AssetsDbContext(
    DbContextOptions<AssetsDbContext> options,
    ICurrentUser currentUser)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetModel> AssetModels => Set<AssetModel>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<AssetStatus> AssetStatuses => Set<AssetStatus>();
    public DbSet<AssetStatusTransition> AssetStatusTransitions => Set<AssetStatusTransition>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<CustomAttributeDefinition> CustomAttributeDefinitions => Set<CustomAttributeDefinition>();
    public DbSet<AssetTypeAttribute> AssetTypeAttributes => Set<AssetTypeAttribute>();
    public DbSet<RfidTag> RfidTags => Set<RfidTag>();
    public DbSet<Barcode> Barcodes => Set<Barcode>();
    public DbSet<AssetImage> AssetImages => Set<AssetImage>();
    public DbSet<ChangeHistoryEntry> ChangeHistory => Set<ChangeHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AuditableEntity>().UseTpcMappingStrategy();

        ConfigureMaster<AssetType>(builder, "AssetTypes");
        ConfigureMaster<AssetCategory>(builder, "AssetCategories");
        ConfigureMaster<AssetModel>(builder, "AssetModels");
        ConfigureMaster<Manufacturer>(builder, "Manufacturers");
        ConfigureMaster<Supplier>(builder, "Suppliers");
        ConfigureMaster<AssetStatus>(builder, "AssetStatuses");
        ConfigureMaster<Asset>(builder, "Assets");
        ConfigureMaster<CustomAttributeDefinition>(builder, "CustomAttributeDefinitions");

        builder.Entity<AssetType>(e =>
        {
            e.Property(x => x.NumberingFormat).HasMaxLength(50);
            e.Property(x => x.PermittedStatusTransitions).HasMaxLength(4000);
            e.Property(x => x.CustomAttributeSchema).HasMaxLength(4000);
            e.Property(x => x.DefaultDepreciationMethod).HasMaxLength(100);
            e.HasOne(x => x.AssetCategory).WithMany().HasForeignKey(x => x.AssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DefaultStatus).WithMany().HasForeignKey(x => x.DefaultStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.AssetCategoryId);
            e.HasIndex(x => x.DefaultStatusId);
        });
        builder.Entity<AssetCategory>(e =>
        {
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AccountCode).HasMaxLength(100);
            e.HasIndex(x => x.ParentId);
            e.HasIndex(x => x.Name).IsUnique();
            var codeIndex = e.HasIndex(x => x.Code).IsUnique();
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
                codeIndex.HasFilter("[Code] IS NOT NULL AND [Code] <> N''");
        });
        builder.Entity<ChangeHistoryEntry>(e =>
        {
            e.ToTable("ChangeHistory");
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Change).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Field).HasMaxLength(100);
            e.Property(x => x.OldValue).HasMaxLength(500);
            e.Property(x => x.NewValue).HasMaxLength(500);
            e.Property(x => x.By).HasMaxLength(256).IsRequired();
            e.Property(x => x.Source).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.WhenUtc });
        });
        builder.Entity<Manufacturer>(e =>
        {
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.SupportContact).HasMaxLength(1000);
            e.Property(x => x.Website).HasMaxLength(500);
            e.HasIndex(x => x.Name).IsUnique();
        });
        builder.Entity<Supplier>(e =>
        {
            e.Property(x => x.SupplierKind).HasMaxLength(50).IsRequired();
            e.Property(x => x.TaxRegistration).HasMaxLength(100);
            e.Property(x => x.ContactPerson).HasMaxLength(200);
            e.Property(x => x.Telephone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.PaymentTerms).HasMaxLength(100);
            e.Property(x => x.Rating).HasMaxLength(50);
            e.Property(x => x.ExternalIdentifier).HasMaxLength(100);
            e.HasIndex(x => x.TaxRegistration).IsUnique().HasFilter("[TaxRegistration] IS NOT NULL");
            e.HasIndex(x => x.ExternalIdentifier).IsUnique().HasFilter("[ExternalIdentifier] IS NOT NULL");
        });
        builder.Entity<AssetModel>(e =>
        {
            e.HasOne(x => x.Manufacturer).WithMany().HasForeignKey(x => x.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssetType).WithMany().HasForeignKey(x => x.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ModelNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Specifications).HasMaxLength(4000);
            e.Property(x => x.Documentation).HasMaxLength(4000);
            e.HasIndex(x => new { x.ManufacturerId, x.ModelNumber }).IsUnique();
        });
        builder.Entity<AssetStatus>(e =>
        {
            e.Property(x => x.StatusCategory).HasMaxLength(50).IsRequired();
            e.Property(x => x.Color).HasMaxLength(7).IsRequired();
            e.HasIndex(x => x.DisplayOrder);
        });
        builder.Entity<AssetStatusTransition>(e =>
        {
            e.HasKey(x => new { x.FromStatusId, x.ToStatusId });
            e.HasOne(x => x.FromStatus).WithMany(x => x.AllowedFrom).HasForeignKey(x => x.FromStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToStatus).WithMany(x => x.AllowedTo).HasForeignKey(x => x.ToStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Asset>(e =>
        {
            e.Property(x => x.AssetNumber).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.AssetNumber).IsUnique();
            e.HasOne(x => x.AssetType).WithMany().HasForeignKey(x => x.AssetTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssetCategory).WithMany().HasForeignKey(x => x.AssetCategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssetModel).WithMany().HasForeignKey(x => x.AssetModelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssetStatus).WithMany().HasForeignKey(x => x.AssetStatusId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CustomAttributeDefinition>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique(false);
            e.Property(x => x.ListValuesJson).HasMaxLength(4000);
            e.Property(x => x.Unit).HasMaxLength(50);
            e.Property(x => x.HelpText).HasMaxLength(1000);
            e.Property(x => x.AlternateHelpText).HasMaxLength(1000);
        });
        builder.Entity<AssetTypeAttribute>(e =>
        {
            ConfigureAuditable(e);
            e.HasIndex(x => new { x.AssetTypeId, x.CustomAttributeDefinitionId }).IsUnique();
            e.HasIndex(x => new { x.AssetTypeId, x.DisplayOrder }).IsUnique();
            e.HasOne(x => x.AssetType).WithMany(x => x.Attributes).HasForeignKey(x => x.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CustomAttributeDefinition).WithMany(x => x.AssetTypes)
                .HasForeignKey(x => x.CustomAttributeDefinitionId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RfidTag>(e =>
        {
            ConfigureAuditable(e);
            e.Property(x => x.TagIdentifier).HasMaxLength(128).IsRequired();
            e.Property(x => x.TagType).HasMaxLength(50).IsRequired();
            e.Property(x => x.EncodingStandard).HasMaxLength(50).IsRequired();
            e.Property(x => x.EncodedBy).HasMaxLength(256);
            e.HasIndex(x => x.TagIdentifier).IsUnique();
            e.HasIndex(x => x.AssetId).IsUnique().HasFilter("[AssetId] IS NOT NULL AND [Status] = 1");
            e.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReplacedBy).WithMany().HasForeignKey(x => x.ReplacedById).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Barcode>(e =>
        {
            ConfigureAuditable(e);
            e.Property(x => x.Value).HasMaxLength(128).IsRequired();
            e.Property(x => x.Symbology).HasMaxLength(30).IsRequired();
            e.Property(x => x.SubjectType).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.Value, x.Symbology }).IsUnique();
            e.HasIndex(x => x.AssetId).IsUnique().HasFilter("[AssetId] IS NOT NULL AND [Status] = 1");
            e.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReplacedBy).WithMany().HasForeignKey(x => x.ReplacedById).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<AssetImage>(e =>
        {
            ConfigureAuditable(e);
            e.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Caption).HasMaxLength(500);
            e.Property(x => x.Purpose).HasMaxLength(100);
            e.Property(x => x.CapturedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.AssetId, x.IsPrimary });
            e.HasOne(x => x.Asset).WithMany(x => x.Images).HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMaster<T>(ModelBuilder builder, string table) where T : CodedMasterEntity
    {
        builder.Entity<T>(e =>
        {
            e.ToTable(table);
            ConfigureAuditable(e);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.AlternateName).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => x.Code).IsUnique();
        });
    }

    private static void ConfigureAuditable<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e)
        where T : AuditableEntity
    {
        e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        e.Property(x => x.UpdatedBy).HasMaxLength(256);
        e.Property(x => x.DeletedBy).HasMaxLength(256);
        e.Property(x => x.RowVersion).IsConcurrencyToken();
        e.HasIndex(x => x.IsActive);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = currentUser.UserName;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = currentUser.UserName;
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string UserName => accessor.HttpContext?.User.Identity?.Name ?? "system";
    public string DisplayName =>
        accessor.HttpContext?.User.FindFirst("display_name")?.Value
        ?? accessor.HttpContext?.User.FindFirst(ClaimTypes.GivenName)?.Value
        ?? UserName;
}

public sealed class LocalFileStorage(IWebHostEnvironment environment) : IFileStorageService
{
    private string Root => Path.Combine(environment.ContentRootPath, "App_Data", "asset-images");

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Root);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(Root, fileName);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(output, cancellationToken);
        return fileName;
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(storedFileName);
        if (!string.Equals(safeName, storedFileName, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid stored file name.");
        Stream stream = new FileStream(Path.Combine(Root, safeName), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(storedFileName);
        if (string.Equals(safeName, storedFileName, StringComparison.Ordinal))
            File.Delete(Path.Combine(Root, safeName));
        return Task.CompletedTask;
    }
}

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";
    public int ExpiryMinutes { get; init; } = 60;
}

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options) : IAuthService
{
    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return null;

        var roles = await userManager.GetRolesAsync(user);
        var expiry = DateTime.UtcNow.AddMinutes(options.Value.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(ClaimTypes.Name, user.UserName!),
            new("display_name", user.DisplayName ?? user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Value.Issuer, options.Value.Audience, claims,
            expires: expiry, signingCredentials: credentials);
        return new(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expiry, user.UserName!, roles.ToArray());
    }
}

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IFileStorageService, LocalFileStorage>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAssetDataService, AssetDataService>();
        services.AddScoped<IAssetCategoryService, AssetCategoryService>();
        services.AddScoped<IManufacturerService, ManufacturerService>();
        services.AddScoped<IAssetTypeService, AssetTypeService>();
        services.AddScoped<IAssetModelService, AssetModelService>();
        services.AddDbContext<AssetsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = false;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<AssetsDbContext>();
        return services;
    }
}

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AssetsDbContext>();
        if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            await db.Database.EnsureCreatedAsync(cancellationToken);
        else
            await db.Database.MigrateAsync(cancellationToken);
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminRole = "Admin";
        if (!await roles.RoleExistsAsync(adminRole))
            EnsureIdentity(await roles.CreateAsync(new IdentityRole(adminRole)));
        var admin = await users.FindByNameAsync("admin");
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = "admin", DisplayName = "Administrator" };
            EnsureIdentity(await users.CreateAsync(admin, "Admin@123"));
        }
        if (!await users.IsInRoleAsync(admin, adminRole))
            EnsureIdentity(await users.AddToRoleAsync(admin, adminRole));

        if (!await db.AssetStatuses.AnyAsync(cancellationToken))
        {
            db.AssetStatuses.AddRange(
                new AssetStatus { Code = "WRK", Name = "Working", StatusCategory = "Working", Color = "#16a34a", IsOperational = true, DisplayOrder = 1 },
                new AssetStatus { Code = "DMG", Name = "Damaged", StatusCategory = "Damaged", Color = "#dc2626", BlocksMovement = true, DisplayOrder = 2 },
                new AssetStatus { Code = "MNT", Name = "In Maintenance", StatusCategory = "In Maintenance", Color = "#f59e0b", DisplayOrder = 3 },
                new AssetStatus { Code = "MIS", Name = "Missing", StatusCategory = "Missing", Color = "#7c3aed", BlocksMovement = true, DisplayOrder = 4 },
                new AssetStatus { Code = "TRN", Name = "In Transit", StatusCategory = "In Transit", Color = "#0891b2", DisplayOrder = 5 },
                new AssetStatus { Code = "DSP", Name = "Disposed", StatusCategory = "Disposed", Color = "#6b7280", IsTerminal = true, BlocksMovement = true, DisplayOrder = 6 });
            await db.SaveChangesAsync(cancellationToken);
        }
        if (!await db.AssetTypes.AnyAsync(cancellationToken))
        {
            var type = new AssetType { Code = "GENERAL", Name = "General Asset", NumberingFormat = "AST-#####" };
            var status = await db.AssetStatuses.FirstAsync(cancellationToken);
            db.AssetTypes.Add(type);
            db.Assets.Add(new Asset
            {
                Code = "AST-DEMO", AssetNumber = "AST-00001", Name = "Demo Asset",
                AssetType = type, AssetStatus = status
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void EnsureIdentity(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
