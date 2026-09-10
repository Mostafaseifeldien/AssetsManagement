using AssetsManagement.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AssetsDbContext))]
    [Migration("20260909172000_StoreMoreInformationFlags")]
    public partial class StoreMoreInformationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MoreInformation",
                table: "AssetCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MoreInformation",
                table: "AssetModels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MoreInformation",
                table: "AssetTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MoreInformation",
                table: "Manufacturers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE AssetCategories SET MoreInformation = 1
                WHERE AlternateName IS NOT NULL OR ParentId IS NOT NULL OR AccountCode IS NOT NULL;
                UPDATE AssetModels SET MoreInformation = 1
                WHERE AlternateName IS NOT NULL OR AssetTypeId IS NOT NULL OR Specifications IS NOT NULL
                   OR ExpectedUsefulLifeMonths IS NOT NULL OR Documentation IS NOT NULL;
                UPDATE AssetTypes SET MoreInformation = 1
                WHERE AlternateName IS NOT NULL OR RequiresRfidTag = 1 OR RequiresBarcode = 1
                   OR PermittedStatusTransitions IS NOT NULL OR CustomAttributeSchema IS NOT NULL
                   OR DefaultDepreciationMethod IS NOT NULL OR DefaultUsefulLifeMonths IS NOT NULL
                   OR NumberingFormat IS NOT NULL;
                UPDATE Manufacturers SET MoreInformation = 1
                WHERE AlternateName IS NOT NULL OR Country IS NOT NULL OR SupportContact IS NOT NULL
                   OR Website IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MoreInformation", table: "AssetCategories");
            migrationBuilder.DropColumn(name: "MoreInformation", table: "AssetModels");
            migrationBuilder.DropColumn(name: "MoreInformation", table: "AssetTypes");
            migrationBuilder.DropColumn(name: "MoreInformation", table: "Manufacturers");
        }
    }
}
