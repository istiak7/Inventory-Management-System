using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleVoidAndRelaxSerialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_ProductSerialId",
                table: "SaleDetails");

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "CustomerSales",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "CustomerSales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ProductSerialId",
                table: "SaleDetails",
                column: "ProductSerialId",
                unique: true,
                filter: "\"Status\" <> 'Voided'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_ProductSerialId",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "CustomerSales");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "CustomerSales");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ProductSerialId",
                table: "SaleDetails",
                column: "ProductSerialId",
                unique: true);
        }
    }
}
