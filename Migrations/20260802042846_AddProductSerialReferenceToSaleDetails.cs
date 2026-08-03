using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSerialReferenceToSaleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductSerialId",
                table: "SaleDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoldDate",
                table: "ProductSerials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ProductSerialId",
                table: "SaleDetails",
                column: "ProductSerialId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_ProductSerials_ProductSerialId",
                table: "SaleDetails",
                column: "ProductSerialId",
                principalTable: "ProductSerials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_ProductSerials_ProductSerialId",
                table: "SaleDetails");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_ProductSerialId",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "ProductSerialId",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "SoldDate",
                table: "ProductSerials");
        }
    }
}
