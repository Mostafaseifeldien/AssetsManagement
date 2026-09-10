using AssetsManagement.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AssetsDbContext))]
    [Migration("20260909215000_AddBarcodeStatusSupplierMoreInformation")]
    public partial class AddBarcodeStatusSupplierMoreInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MoreInformation",
                table: "Barcodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MoreInformation",
                table: "AssetStatuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MoreInformation",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MoreInformation", table: "Barcodes");
            migrationBuilder.DropColumn(name: "MoreInformation", table: "AssetStatuses");
            migrationBuilder.DropColumn(name: "MoreInformation", table: "Suppliers");
        }
    }
}
