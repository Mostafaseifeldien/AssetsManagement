using AssetsManagement.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetsManagement.Infrastructure;

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
        await EnsureEmptyAssetModelNumbersAreAllowedAsync(db, cancellationToken);
        await EnsureEmptyMasterCodesAreAllowedAsync(db, cancellationToken);
        await EnsureMoreInformationColumnsAsync(db, cancellationToken);
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
                new AssetStatus { Code = "MNT", Name = "In Maintenance", StatusCategory = "In Maintenance", Color = "#f59e0b", BlocksMovement = true, DisplayOrder = 3 },
                new AssetStatus { Code = "MIS", Name = "Missing", StatusCategory = "Missing", Color = "#7c3aed", BlocksMovement = true, DisplayOrder = 4 },
                new AssetStatus { Code = "TRN", Name = "In Transit", StatusCategory = "In Transit", Color = "#0891b2", IsOperational = true, DisplayOrder = 5 },
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

    private static async Task EnsureEmptyAssetModelNumbersAreAllowedAsync(
        AssetsDbContext db, CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
            return;
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.AssetModels', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_AssetModels_ManufacturerId_ModelNumber'
                      AND object_id = OBJECT_ID(N'dbo.AssetModels'))
                    DROP INDEX [IX_AssetModels_ManufacturerId_ModelNumber] ON [dbo].[AssetModels];
                CREATE UNIQUE INDEX [IX_AssetModels_ManufacturerId_ModelNumber]
                ON [dbo].[AssetModels]([ManufacturerId], [ModelNumber])
                WHERE [ModelNumber] IS NOT NULL AND [ModelNumber] <> N'';
            END
            """, cancellationToken);
    }

    private static async Task EnsureEmptyMasterCodesAreAllowedAsync(
        AssetsDbContext db, CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
            return;
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.AssetTypes', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_AssetTypes_Code'
                      AND object_id = OBJECT_ID(N'dbo.AssetTypes'))
                    DROP INDEX [IX_AssetTypes_Code] ON [dbo].[AssetTypes];
                CREATE UNIQUE INDEX [IX_AssetTypes_Code]
                ON [dbo].[AssetTypes]([Code])
                WHERE [Code] IS NOT NULL AND [Code] <> N'';
            END
            IF OBJECT_ID(N'dbo.AssetCategories', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_AssetCategories_Code'
                      AND object_id = OBJECT_ID(N'dbo.AssetCategories'))
                    DROP INDEX [IX_AssetCategories_Code] ON [dbo].[AssetCategories];
                CREATE UNIQUE INDEX [IX_AssetCategories_Code]
                ON [dbo].[AssetCategories]([Code])
                WHERE [Code] IS NOT NULL AND [Code] <> N'';
            END
            IF OBJECT_ID(N'dbo.Manufacturers', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Manufacturers_Code'
                      AND object_id = OBJECT_ID(N'dbo.Manufacturers'))
                    DROP INDEX [IX_Manufacturers_Code] ON [dbo].[Manufacturers];
                CREATE UNIQUE INDEX [IX_Manufacturers_Code]
                ON [dbo].[Manufacturers]([Code])
                WHERE [Code] IS NOT NULL AND [Code] <> N'';
            END
            """, cancellationToken);
    }

    private static async Task EnsureMoreInformationColumnsAsync(
        AssetsDbContext db, CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
            return;
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.AssetCategories', N'MoreInformation') IS NULL
                ALTER TABLE [dbo].[AssetCategories] ADD [MoreInformation] bit NOT NULL
                    CONSTRAINT [DF_AssetCategories_MoreInformation] DEFAULT(0);
            IF COL_LENGTH(N'dbo.AssetModels', N'MoreInformation') IS NULL
                ALTER TABLE [dbo].[AssetModels] ADD [MoreInformation] bit NOT NULL
                    CONSTRAINT [DF_AssetModels_MoreInformation] DEFAULT(0);
            IF COL_LENGTH(N'dbo.AssetTypes', N'MoreInformation') IS NULL
                ALTER TABLE [dbo].[AssetTypes] ADD [MoreInformation] bit NOT NULL
                    CONSTRAINT [DF_AssetTypes_MoreInformation] DEFAULT(0);
            IF COL_LENGTH(N'dbo.Manufacturers', N'MoreInformation') IS NULL
                ALTER TABLE [dbo].[Manufacturers] ADD [MoreInformation] bit NOT NULL
                    CONSTRAINT [DF_Manufacturers_MoreInformation] DEFAULT(0);
            """, cancellationToken);
    }

    private static void EnsureIdentity(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
