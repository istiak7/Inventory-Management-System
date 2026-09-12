using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddReportQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierTransactions_SupplierId",
                table: "SupplierTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_BranchId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_CustomerId",
                table: "CustomerTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTransactions_SupplierId_TransactionDate",
                table: "SupplierTransactions",
                columns: new[] { "SupplierId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_BranchId_ProductVariantId_Transaction~",
                table: "InventoryTransactions",
                columns: new[] { "BranchId", "ProductVariantId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_CustomerId_TransactionDate",
                table: "CustomerTransactions",
                columns: new[] { "CustomerId", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierTransactions_SupplierId_TransactionDate",
                table: "SupplierTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_BranchId_ProductVariantId_Transaction~",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_CustomerId_TransactionDate",
                table: "CustomerTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTransactions_SupplierId",
                table: "SupplierTransactions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_BranchId",
                table: "InventoryTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_CustomerId",
                table: "CustomerTransactions",
                column: "CustomerId");
        }
    }
}
