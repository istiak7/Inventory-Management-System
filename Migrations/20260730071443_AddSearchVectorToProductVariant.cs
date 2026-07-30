using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchVectorToProductVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ১. SearchText — application-owned plain text projection (AppDbContext.SaveChanges).
            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "ProductVariants",
                type: "text",
                nullable: true);

            // ২. SearchVector — added NULLABLE on purpose. Existing rows have no vector yet, so
            //    a NOT NULL column with no default would abort the migration
            //    ("column SearchVector contains null values"). It is tightened to NOT NULL in
            //    step ৫, once the backfill has populated every row.
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "ProductVariants",
                type: "tsvector",
                nullable: true);

            // ৩. Trigger Function — SearchText থেকে SearchVector compute করবে
            migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION update_productvariant_searchvector()
            RETURNS trigger AS $$
            BEGIN
                NEW.""SearchVector"" := to_tsvector('english', COALESCE(NEW.""SearchText"", ''));
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
        ");

            // ৪. Trigger — Insert/Update এর আগে চলবে. Created before the backfill so the
            //    backfill's UPDATE is what populates SearchVector — one code path, no drift.
            migrationBuilder.Sql(@"
            DROP TRIGGER IF EXISTS trg_productvariant_searchvector ON ""ProductVariants"";
            CREATE TRIGGER trg_productvariant_searchvector
            BEFORE INSERT OR UPDATE OF ""SearchText"" ON ""ProductVariants""
            FOR EACH ROW EXECUTE FUNCTION update_productvariant_searchvector();
        ");

            // ৫. Backfill existing rows. Mirrors ProductVariant.RebuildSearchText:
            //    product name + SKU + barcode. (Attribute values are picked up the next time a
            //    variant is saved; they are not worth a jsonb walk in SQL here.)
            migrationBuilder.Sql(@"
            UPDATE ""ProductVariants"" v
            SET ""SearchText"" = btrim(concat_ws(' ',
                NULLIF(p.""ProductName"", ''),
                NULLIF(v.""SKU"", ''),
                NULLIF(v.""Barcode"", '')))
            FROM ""Products"" p
            WHERE p.""Id"" = v.""ProductId"";
        ");

            // Safety net: any row the join above missed still needs a non-null vector before
            // the NOT NULL constraint goes on. Touching SearchText fires the trigger.
            migrationBuilder.Sql(@"
            UPDATE ""ProductVariants""
            SET ""SearchText"" = COALESCE(""SearchText"", '')
            WHERE ""SearchVector"" IS NULL;
        ");

            migrationBuilder.Sql(@"ALTER TABLE ""ProductVariants"" ALTER COLUMN ""SearchVector"" SET NOT NULL;");

            // ৬. GIN index last — building it once over a populated column is far cheaper than
            //    maintaining it row-by-row through the backfill above.
            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SearchVector",
                table: "ProductVariants",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_productvariant_searchvector ON ""ProductVariants"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_productvariant_searchvector();");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_SearchVector",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "ProductVariants");
        }
    }
}
