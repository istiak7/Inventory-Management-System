using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class SalesUnitCostAndDebitSaleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "SaleDetails",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // SaleType is stored as its enum name (HasConversion<string>), so renaming
            // SaleType.Credit to SaleType.Debit would leave existing rows holding a value the
            // enum no longer knows. Column shape is unchanged - this is a data fix.
            migrationBuilder.Sql(
                """
                UPDATE "CustomerSales"
                SET "SaleType" = 'Debit'
                WHERE "SaleType" = 'Credit';
                """);

            // Backfill the cost of sales made before this column existed, so historical gross
            // profit is approximately right instead of showing the full sale price as profit.
            //
            // Step 1 - serialized units: the serial records the exact purchase line it arrived
            // on, so we can use what was really paid for that unit.
            migrationBuilder.Sql(
                """
                UPDATE "SaleDetails" sd
                SET "UnitCost" = spd."UnitPrice"
                FROM "ProductSerials" ps
                JOIN "SupplierPurchaseDetails" spd ON spd."Id" = ps."SupplierPurchaseDetailsId"
                WHERE sd."ProductSerialId" = ps."Id"
                  AND sd."UnitCost" = 0;
                """);

            // Step 2 - everything else: weighted average over the quantity actually received,
            // matching what ProductCostResolver does for new sales. This is an estimate: the
            // real cost at the time of each historical sale is not recoverable.
            migrationBuilder.Sql(
                """
                UPDATE "SaleDetails" sd
                SET "UnitCost" = avg_cost.value
                FROM (
                    SELECT "ProductVariantId",
                           ROUND(SUM("UnitPrice" * "ReceivedQuantity")
                                 / NULLIF(SUM("ReceivedQuantity"), 0), 2) AS value
                    FROM "SupplierPurchaseDetails"
                    WHERE "ReceivedQuantity" IS NOT NULL AND "ReceivedQuantity" > 0
                    GROUP BY "ProductVariantId"
                ) AS avg_cost
                WHERE sd."ProductVariantId" = avg_cost."ProductVariantId"
                  AND avg_cost.value IS NOT NULL
                  AND sd."UnitCost" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "CustomerSales"
                SET "SaleType" = 'Credit'
                WHERE "SaleType" = 'Debit';
                """);

            // The backfilled costs go with the column.
            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "SaleDetails");
        }
    }
}
