using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionedDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenter",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Criticality",
                table: "Assets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentCustodianId",
                table: "Assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentLocation",
                table: "Assets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustodianType",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomAttributesJson",
                table: "Assets",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepreciationMethod",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisposalDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisposalReason",
                table: "Assets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAtUtc",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSeenReader",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationSource",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationUpdatedAtUtc",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                table: "Assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwningDepartment",
                table: "Assets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwningOrganization",
                table: "Assets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentAssetId",
                table: "Assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseReference",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseValue",
                table: "Assets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResidualValue",
                table: "Assets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsefulLifeMonths",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiry",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    DocumentKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Confidential = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetDocuments_AssetDocuments_SupersededById",
                        column: x => x.SupersededById,
                        principalTable: "AssetDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SourceAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetRelationships_Assets_SourceAssetId",
                        column: x => x.SourceAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetRelationships_Assets_TargetAssetId",
                        column: x => x.TargetAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustodyTransferBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustodianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustodianType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssignedFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequireAcknowledgement = table.Column<bool>(type: "bit", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssetCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyTransferBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AlternateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetDocumentLinks",
                columns: table => new
                {
                    AssetDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDocumentLinks", x => new { x.AssetDocumentId, x.AssetId });
                    table.ForeignKey(
                        name: "FK_AssetDocumentLinks_AssetDocuments_AssetDocumentId",
                        column: x => x.AssetDocumentId,
                        principalTable: "AssetDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetDocumentLinks_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustodyAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustodianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustodianType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Acknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HandoverDocument = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodyAssignments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyAssignments_CustodyTransferBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "CustodyTransferBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CurrentCustodianId",
                table: "Assets",
                column: "CurrentCustodianId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_LastSeenAtUtc",
                table: "Assets",
                column: "LastSeenAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ManufacturerId_SerialNumber",
                table: "Assets",
                columns: new[] { "ManufacturerId", "SerialNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ParentAssetId",
                table: "Assets",
                column: "ParentAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocumentLinks_AssetId",
                table: "AssetDocumentLinks",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_ExpiresOn",
                table: "AssetDocuments",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_IsActive",
                table: "AssetDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_SupersededById",
                table: "AssetDocuments",
                column: "SupersededById");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRelationships_IsActive",
                table: "AssetRelationships",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRelationships_SourceAssetId_TargetAssetId_RelationshipType_ValidFromUtc",
                table: "AssetRelationships",
                columns: new[] { "SourceAssetId", "TargetAssetId", "RelationshipType", "ValidFromUtc" },
                unique: true,
                filter: "[SourceAssetId] IS NOT NULL AND [TargetAssetId] IS NOT NULL AND [RelationshipType] IS NOT NULL AND [ValidFromUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRelationships_TargetAssetId",
                table: "AssetRelationships",
                column: "TargetAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyAssignments_AssetId_Status",
                table: "CustodyAssignments",
                columns: new[] { "AssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyAssignments_BatchId",
                table: "CustodyAssignments",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyAssignments_IsActive",
                table: "CustodyAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyTransferBatches_BatchNumber",
                table: "CustodyTransferBatches",
                column: "BatchNumber",
                unique: true,
                filter: "[BatchNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyTransferBatches_IsActive",
                table: "CustodyTransferBatches",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Code",
                table: "Employees",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsActive",
                table: "Employees",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Assets_ParentAssetId",
                table: "Assets",
                column: "ParentAssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Employees_CurrentCustodianId",
                table: "Assets",
                column: "CurrentCustodianId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Manufacturers_ManufacturerId",
                table: "Assets",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Assets_ParentAssetId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Employees_CurrentCustodianId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Manufacturers_ManufacturerId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "AssetDocumentLinks");

            migrationBuilder.DropTable(
                name: "AssetRelationships");

            migrationBuilder.DropTable(
                name: "CustodyAssignments");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "AssetDocuments");

            migrationBuilder.DropTable(
                name: "CustodyTransferBatches");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CurrentCustodianId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_LastSeenAtUtc",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_ManufacturerId_SerialNumber",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_ParentAssetId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CommissionedDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CostCenter",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Criticality",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CurrentCustodianId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CustodianType",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CustomAttributesJson",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DepreciationMethod",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DisposalDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DisposalReason",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LastSeenAtUtc",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LastSeenReader",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LocationSource",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LocationUpdatedAtUtc",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "OwningDepartment",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "OwningOrganization",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ParentAssetId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PurchaseReference",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PurchaseValue",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ResidualValue",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "UsefulLifeMonths",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiry",
                table: "Assets");
        }
    }
}
