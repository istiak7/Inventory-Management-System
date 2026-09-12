using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseTypeChosenOnApproval : Migration
    {
        // The Cash/Debit payment type is no longer picked when a purchase order is raised - it
        // is picked when the order is approved. A pending order therefore has no payment type
        // yet, so the column has to accept null.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PurchaseType",
                table: "SupplierPurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "SupplierPurchases"
                SET "PurchaseType" = 'Debit'
                WHERE "PurchaseType" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PurchaseType",
                table: "SupplierPurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
