using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Infrastructure;

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
