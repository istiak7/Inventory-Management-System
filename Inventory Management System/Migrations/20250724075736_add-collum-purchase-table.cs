using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class addcollumpurchasetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Purchases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_WarehouseId",
                table: "Purchases",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_WarehouseId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Purchases");
        }
    }
}
