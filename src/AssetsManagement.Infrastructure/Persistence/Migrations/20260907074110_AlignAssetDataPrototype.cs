using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignAssetDataPrototype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomAttributeDefinitions_Code",
                table: "CustomAttributeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Barcodes_Value",
                table: "Barcodes");

            migrationBuilder.DropIndex(
                name: "IX_AssetModels_ManufacturerId_Name",
                table: "AssetModels");

            migrationBuilder.AddColumn<string>(
                name: "SupportContact",
                table: "Manufacturers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssetCategoryId",
                table: "AssetTypes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomAttributeSchema",
                table: "AssetTypes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDepreciationMethod",
                table: "AssetTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultStatusId",
                table: "AssetTypes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultUsefulLifeMonths",
                table: "AssetTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermittedStatusTransitions",
                table: "AssetTypes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresBarcode",
                table: "AssetTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresRfidTag",
                table: "AssetTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSerialNumber",
                table: "AssetTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlocksMovement",
                table: "AssetStatuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "AssetStatuses",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#6b7280");

            migrationBuilder.AddColumn<bool>(
                name: "IsTerminal",
                table: "AssetStatuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StatusCategory",
                table: "AssetStatuses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "Documentation",
                table: "AssetModels",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedUsefulLifeMonths",
                table: "AssetModels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelNumber",
                table: "AssetModels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Specifications",
                table: "AssetModels",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountCode",
                table: "AssetCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_Name",
                table: "Manufacturers",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomAttributeDefinitions_Code",
                table: "CustomAttributeDefinitions",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Barcodes_Value_Symbology",
                table: "Barcodes",
                columns: new[] { "Value", "Symbology" },
                unique: true,
                filter: "[Value] IS NOT NULL AND [Symbology] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_AssetCategoryId",
                table: "AssetTypes",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_DefaultStatusId",
                table: "AssetTypes",
                column: "DefaultStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetModels_ManufacturerId_ModelNumber",
                table: "AssetModels",
                columns: new[] { "ManufacturerId", "ModelNumber" },
                unique: true,
                filter: "[ManufacturerId] IS NOT NULL AND [ModelNumber] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTypes_AssetCategories_AssetCategoryId",
                table: "AssetTypes",
                column: "AssetCategoryId",
                principalTable: "AssetCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTypes_AssetStatuses_DefaultStatusId",
                table: "AssetTypes",
                column: "DefaultStatusId",
                principalTable: "AssetStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetTypes_AssetCategories_AssetCategoryId",
                table: "AssetTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetTypes_AssetStatuses_DefaultStatusId",
                table: "AssetTypes");

            migrationBuilder.DropIndex(
                name: "IX_Manufacturers_Name",
                table: "Manufacturers");

            migrationBuilder.DropIndex(
                name: "IX_CustomAttributeDefinitions_Code",
                table: "CustomAttributeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Barcodes_Value_Symbology",
                table: "Barcodes");

            migrationBuilder.DropIndex(
                name: "IX_AssetTypes_AssetCategoryId",
                table: "AssetTypes");

            migrationBuilder.DropIndex(
                name: "IX_AssetTypes_DefaultStatusId",
                table: "AssetTypes");

            migrationBuilder.DropIndex(
                name: "IX_AssetModels_ManufacturerId_ModelNumber",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "SupportContact",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "AssetCategoryId",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "CustomAttributeSchema",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "DefaultDepreciationMethod",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "DefaultStatusId",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "DefaultUsefulLifeMonths",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "PermittedStatusTransitions",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "RequiresBarcode",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "RequiresRfidTag",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "RequiresSerialNumber",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "BlocksMovement",
                table: "AssetStatuses");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "AssetStatuses");

            migrationBuilder.DropColumn(
                name: "IsTerminal",
                table: "AssetStatuses");

            migrationBuilder.DropColumn(
                name: "StatusCategory",
                table: "AssetStatuses");

            migrationBuilder.DropColumn(
                name: "Documentation",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "ExpectedUsefulLifeMonths",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "ModelNumber",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "AccountCode",
                table: "AssetCategories");

            migrationBuilder.CreateIndex(
                name: "IX_CustomAttributeDefinitions_Code",
                table: "CustomAttributeDefinitions",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Barcodes_Value",
                table: "Barcodes",
                column: "Value",
                unique: true,
                filter: "[Value] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetModels_ManufacturerId_Name",
                table: "AssetModels",
                columns: new[] { "ManufacturerId", "Name" },
                unique: true,
                filter: "[ManufacturerId] IS NOT NULL AND [Name] IS NOT NULL");
        }
    }
}
