using AssetsManagement.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AssetsDbContext))]
    [Migration("20260909164000_AllowEmptyAssetTypeCode")]
    public partial class AllowEmptyAssetTypeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetTypes_Code",
                table: "AssetTypes");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_Code",
                table: "AssetTypes",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> N''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetTypes_Code",
                table: "AssetTypes");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_Code",
                table: "AssetTypes",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");
        }
    }
}
