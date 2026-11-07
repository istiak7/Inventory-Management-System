using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class WarrantyMonthsToDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WarrantyMonths",
                table: "SupplierPurchaseDetails",
                newName: "WarrantyDays");

            migrationBuilder.RenameColumn(
                name: "WarrantyMonths",
                table: "SaleDetails",
                newName: "WarrantyDays");

            migrationBuilder.RenameColumn(
                name: "WarrantyMonths",
                table: "ProductSerials",
                newName: "WarrantyDays");

            // Existing terms were months. Whole years become 365 days per year (12 -> 365,
            // 24 -> 730); anything else is 30 days per month (6 -> 180). 0 and NULL stay as they are.
            foreach (var table in new[] { "SupplierPurchaseDetails", "SaleDetails", "ProductSerials" })
            {
                migrationBuilder.Sql($@"
UPDATE ""{table}""
SET ""WarrantyDays"" = CASE
    WHEN ""WarrantyDays"" % 12 = 0 THEN (""WarrantyDays"" / 12) * 365
    ELSE ""WarrantyDays"" * 30
END
WHERE ""WarrantyDays"" > 0;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back to months: multiples of 365 return to whole years, the rest round to the
            // nearest 30-day month (at least 1 for any warranty).
            foreach (var table in new[] { "SupplierPurchaseDetails", "SaleDetails", "ProductSerials" })
            {
                migrationBuilder.Sql($@"
UPDATE ""{table}""
SET ""WarrantyDays"" = CASE
    WHEN ""WarrantyDays"" % 365 = 0 THEN (""WarrantyDays"" / 365) * 12
    ELSE GREATEST(1, ROUND(""WarrantyDays"" / 30.0))::int
END
WHERE ""WarrantyDays"" > 0;");
            }

            migrationBuilder.RenameColumn(
                name: "WarrantyDays",
                table: "SupplierPurchaseDetails",
                newName: "WarrantyMonths");

            migrationBuilder.RenameColumn(
                name: "WarrantyDays",
                table: "SaleDetails",
                newName: "WarrantyMonths");

            migrationBuilder.RenameColumn(
                name: "WarrantyDays",
                table: "ProductSerials",
                newName: "WarrantyMonths");
        }
    }
}
