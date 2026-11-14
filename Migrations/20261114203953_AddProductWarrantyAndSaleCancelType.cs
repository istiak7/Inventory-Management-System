using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddProductWarrantyAndSaleCancelType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // The customer ledger's cancel row is now "SaleCancel" (like the supplier ledger's
            // "PurchaseCancel"); rename the rows already written under the old "Void" type.
            migrationBuilder.Sql("UPDATE \"CustomerTransactions\" SET \"TransactionType\" = 'SaleCancel' WHERE \"TransactionType\" = 'Void';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"CustomerTransactions\" SET \"TransactionType\" = 'Void' WHERE \"TransactionType\" = 'SaleCancel';");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "Products");
        }
    }
}
