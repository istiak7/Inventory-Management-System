using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class saleManagerTableAddu2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesManager_Warehouses_WarehouseId",
                table: "SalesManager");

            migrationBuilder.DropIndex(
                name: "IX_SalesManager_WarehouseId",
                table: "SalesManager");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "SalesManager");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "SalesManager",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalesManager_WarehouseId",
                table: "SalesManager",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesManager_Warehouses_WarehouseId",
                table: "SalesManager",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
