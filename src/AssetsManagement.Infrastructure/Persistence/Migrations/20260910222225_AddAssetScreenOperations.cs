using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetScreenOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetInspections",
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
                    InspectionKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InspectedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InspectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LocationConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CustodianConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PhotographId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionRequired = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextDue = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetInspections_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetPositions",
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
                    FloorPlan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Room = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    X = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Y = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PositionSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DerivedFromReader = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Valid = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetPositions_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepreciationSchedules",
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
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AcquisitionValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResidualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsefulLifeMonths = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    PeriodLength = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetBookValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepreciationSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepreciationSchedules_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExitAuthorizations",
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
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExpectedReturn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CarrierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GateScope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerDecision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OwnerDecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManagerApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManagerDecision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ManagerDecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecurityApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecurityDecision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SecurityDecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExitAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExitAuthorizations_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenancePlans",
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
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScopeKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggerKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaskList = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IntervalMonths = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warranties",
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
                    WarrantyKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Coverage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Exclusions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseTime = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warranties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warranties_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
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
                    WorkOrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnderWarranty = table.Column<bool>(type: "bit", nullable: false),
                    AssetOutOfService = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DowntimeHours = table.Column<decimal>(type: "decimal(18,1)", nullable: false),
                    WorkDone = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepreciationEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Charge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepreciationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepreciationEntries_DepreciationSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "DepreciationSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyClaims",
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
                    ClaimNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarrantyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RaisedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RaisedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FaultDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AmountClaimed = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountRecovered = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClosedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetExpenses",
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
                    ExpenseKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Capitalized = table.Column<bool>(type: "bit", nullable: false),
                    CostCenter = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetExpenses_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetExpenses_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetExpenses_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
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
                    RequestNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RaisedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RaisedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FaultDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssetUsable = table.Column<bool>(type: "bit", nullable: false),
                    PhotographId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationAtReport = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriagedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    LineKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LineCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Chargeable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderLines_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetExpenses_AssetId_ExpenseDate",
                table: "AssetExpenses",
                columns: new[] { "AssetId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetExpenses_IsActive",
                table: "AssetExpenses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetExpenses_SupplierId",
                table: "AssetExpenses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetExpenses_WorkOrderId",
                table: "AssetExpenses",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetInspections_AssetId_InspectedOn",
                table: "AssetInspections",
                columns: new[] { "AssetId", "InspectedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetInspections_IsActive",
                table: "AssetInspections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPositions_AssetId_IsCurrent",
                table: "AssetPositions",
                columns: new[] { "AssetId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPositions_IsActive",
                table: "AssetPositions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DepreciationEntries_ScheduleId_Period",
                table: "DepreciationEntries",
                columns: new[] { "ScheduleId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepreciationSchedules_AssetId_State",
                table: "DepreciationSchedules",
                columns: new[] { "AssetId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_DepreciationSchedules_IsActive",
                table: "DepreciationSchedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExitAuthorizations_AssetId",
                table: "ExitAuthorizations",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExitAuthorizations_IsActive",
                table: "ExitAuthorizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExitAuthorizations_Number",
                table: "ExitAuthorizations",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_Code",
                table: "MaintenancePlans",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_IsActive",
                table: "MaintenancePlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_AssetId",
                table: "MaintenanceRequests",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_IsActive",
                table: "MaintenanceRequests",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_RequestNumber",
                table: "MaintenanceRequests",
                column: "RequestNumber",
                unique: true,
                filter: "[RequestNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_WorkOrderId",
                table: "MaintenanceRequests",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_AssetId_WarrantyKind_StartDate",
                table: "Warranties",
                columns: new[] { "AssetId", "WarrantyKind", "StartDate" },
                unique: true,
                filter: "[State] <> N'Void'");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_IsActive",
                table: "Warranties",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_AssetId",
                table: "WarrantyClaims",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ClaimNumber",
                table: "WarrantyClaims",
                column: "ClaimNumber",
                unique: true,
                filter: "[ClaimNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_IsActive",
                table: "WarrantyClaims",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_WarrantyId",
                table: "WarrantyClaims",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderLines_WorkOrderId_LineNumber",
                table: "WorkOrderLines",
                columns: new[] { "WorkOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AssetId",
                table: "WorkOrders",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_IsActive",
                table: "WorkOrders",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkOrderNumber",
                table: "WorkOrders",
                column: "WorkOrderNumber",
                unique: true,
                filter: "[WorkOrderNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetExpenses");

            migrationBuilder.DropTable(
                name: "AssetInspections");

            migrationBuilder.DropTable(
                name: "AssetPositions");

            migrationBuilder.DropTable(
                name: "DepreciationEntries");

            migrationBuilder.DropTable(
                name: "ExitAuthorizations");

            migrationBuilder.DropTable(
                name: "MaintenancePlans");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "WarrantyClaims");

            migrationBuilder.DropTable(
                name: "WorkOrderLines");

            migrationBuilder.DropTable(
                name: "DepreciationSchedules");

            migrationBuilder.DropTable(
                name: "Warranties");

            migrationBuilder.DropTable(
                name: "WorkOrders");
        }
    }
}
