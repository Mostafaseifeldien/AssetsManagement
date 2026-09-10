using AssetsManagement.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AssetsDbContext))]
    [Migration("20260909170000_AllowEmptyCategoryAndManufacturerCodes")]
    public partial class AllowEmptyCategoryAndManufacturerCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetCategories_Code",
                table: "AssetCategories");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_Code",
                table: "AssetCategories",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");

            migrationBuilder.DropIndex(
                name: "IX_Manufacturers_Code",
                table: "Manufacturers");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_Code",
                table: "Manufacturers",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetCategories_Code",
                table: "AssetCategories");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_Code",
                table: "AssetCategories",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");

            migrationBuilder.DropIndex(
                name: "IX_Manufacturers_Code",
                table: "Manufacturers");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_Code",
                table: "Manufacturers",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");
        }
    }
}
