using AssetsManagement.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AssetsDbContext))]
    [Migration("20260909154600_AllowEmptyAssetModelNumber")]
    public partial class AllowEmptyAssetModelNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetModels_ManufacturerId_ModelNumber",
                table: "AssetModels");

            migrationBuilder.CreateIndex(
                name: "IX_AssetModels_ManufacturerId_ModelNumber",
                table: "AssetModels",
                columns: new[] { "ManufacturerId", "ModelNumber" },
                unique: true,
                filter: "[ModelNumber] IS NOT NULL AND [ModelNumber] <> N''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetModels_ManufacturerId_ModelNumber",
                table: "AssetModels");

            migrationBuilder.CreateIndex(
                name: "IX_AssetModels_ManufacturerId_ModelNumber",
                table: "AssetModels",
                columns: new[] { "ManufacturerId", "ModelNumber" },
                unique: true,
                filter: "[ManufacturerId] IS NOT NULL AND [ModelNumber] IS NOT NULL");
        }
    }
}
