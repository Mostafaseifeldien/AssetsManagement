using AssetsManagement.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetsManagement.Infrastructure;

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
        services.AddScoped<IRfidTagService, RfidTagService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IAssetStatusService, AssetStatusService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IAssetImageService, AssetImageService>();
        services.AddScoped<ICustomAttributeDefinitionService, CustomAttributeDefinitionService>();
        services.AddScoped<IAssetTypeAttributeService, AssetTypeAttributeService>();
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
