using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderVerifiedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedById",
                table: "WorkOrders",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "WorkOrders");
        }
    }
}
