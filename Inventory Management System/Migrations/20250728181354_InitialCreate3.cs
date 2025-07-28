using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_PurchaseDetails_PurchaseId",
                table: "Stocks");

            migrationBuilder.RenameColumn(
                name: "PurchaseId",
                table: "Stocks",
                newName: "PurchaseDetailsId");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_PurchaseId",
                table: "Stocks",
                newName: "IX_Stocks_PurchaseDetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_PurchaseDetails_PurchaseDetailsId",
                table: "Stocks",
                column: "PurchaseDetailsId",
                principalTable: "PurchaseDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_PurchaseDetails_PurchaseDetailsId",
                table: "Stocks");

            migrationBuilder.RenameColumn(
                name: "PurchaseDetailsId",
                table: "Stocks",
                newName: "PurchaseId");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_PurchaseDetailsId",
                table: "Stocks",
                newName: "IX_Stocks_PurchaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_PurchaseDetails_PurchaseId",
                table: "Stocks",
                column: "PurchaseId",
                principalTable: "PurchaseDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
