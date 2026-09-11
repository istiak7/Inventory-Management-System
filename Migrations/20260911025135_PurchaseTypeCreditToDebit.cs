using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseTypeCreditToDebit : Migration
    {
        // PurchaseType is stored as its enum name (HasConversion<string>), so renaming
        // PurchaseType.Credit to PurchaseType.Debit leaves existing rows holding a value the
        // enum no longer knows. The column itself is unchanged - this is a data fix only.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "SupplierPurchases"
                SET "PurchaseType" = 'Debit'
                WHERE "PurchaseType" = 'Credit';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "SupplierPurchases"
                SET "PurchaseType" = 'Credit'
                WHERE "PurchaseType" = 'Debit';
                """);
        }
    }
}
