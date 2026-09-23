using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class StockNotNegativeCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOT VALID: the rule applies to every new change, but old rows are not checked,
            // so a database that already has a negative stock row can still start.
            migrationBuilder.Sql(
                "ALTER TABLE \"Stocks\" ADD CONSTRAINT \"CK_Stocks_CurrentStock_NotNegative\" " +
                "CHECK (\"CurrentStock\" >= 0) NOT VALID;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Stocks_CurrentStock_NotNegative",
                table: "Stocks");
        }
    }
}
