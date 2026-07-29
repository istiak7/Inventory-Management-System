using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPhoneUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers");

            // Bring rows written before the normalizer existed into the same shape the application
            // now stores, in the same three steps as CustomerPhoneNumber.Normalize. Without this,
            // a customer saved as "01712-345678" would never be found by a lookup for
            // "01712345678" and the till would register them a second time.

            // 1. Digits only — drops dashes, spaces, parentheses and a leading '+'.
            migrationBuilder.Sql(@"UPDATE ""Customers"" SET ""PhoneNumber"" = regexp_replace(""PhoneNumber"", '\D', '', 'g');");

            // 2. Drop the '00' international dialling prefix.
            migrationBuilder.Sql(@"UPDATE ""Customers"" SET ""PhoneNumber"" = substring(""PhoneNumber"" from 3) WHERE ""PhoneNumber"" LIKE '00%';");

            // 3. Collapse the Bangladesh country code to the local 0-leading form.
            migrationBuilder.Sql(@"UPDATE ""Customers"" SET ""PhoneNumber"" = '0' || substring(""PhoneNumber"" from 4) WHERE length(""PhoneNumber"") = 13 AND ""PhoneNumber"" LIKE '880%';");

            // If normalizing collapsed two spellings of one number onto each other, this index
            // fails and the whole migration rolls back. That is deliberate: those rows are the
            // same customer recorded twice, and merging them (which sales and ledger rows point at
            // which) is a decision only the operator can make — not something to resolve silently.
            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the index is reversible. The original punctuation is not recoverable — and is
            // not worth recovering, since every reader now expects the normalized form.
            migrationBuilder.DropIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber");
        }
    }
}
