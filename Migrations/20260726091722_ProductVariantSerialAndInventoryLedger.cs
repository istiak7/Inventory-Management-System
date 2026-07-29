using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class ProductVariantSerialAndInventoryLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_SupplierPurchases_SupplierPurchaseId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_Products_ProductId",
                table: "Stocks");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchaseDetails_Products_ProductId",
                table: "SupplierPurchaseDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ProductPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "SupplierPurchaseDetails",
                newName: "WarrantyMonths");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "SupplierPurchaseDetails",
                newName: "ProductVariantId");

            migrationBuilder.RenameColumn(
                name: "IsApproved",
                table: "SupplierPurchaseDetails",
                newName: "Status");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchaseDetails_ProductId",
                table: "SupplierPurchaseDetails",
                newName: "IX_SupplierPurchaseDetails_ProductVariantId");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Stocks",
                newName: "ProductVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_ProductId",
                table: "Stocks",
                newName: "IX_Stocks_ProductVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_BranchId_ProductId",
                table: "Stocks",
                newName: "IX_Stocks_BranchId_ProductVariantId");

            migrationBuilder.RenameColumn(
                name: "SupplierPurchaseId",
                table: "InventoryTransactions",
                newName: "SupplierPurchaseDetailsId");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "InventoryTransactions",
                newName: "QuantityOut");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "InventoryTransactions",
                newName: "QuantityIn");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_SupplierPurchaseId",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_SupplierPurchaseDetailsId");

            migrationBuilder.AlterColumn<string>(
                name: "PurchaseType",
                table: "SupplierPurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "OrderedQuantity",
                table: "SupplierPurchaseDetails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedQuantity",
                table: "SupplierPurchaseDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BalanceAfter",
                table: "InventoryTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "InventoryTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsSerialized = table.Column<bool>(type: "boolean", nullable: false),
                    AttributesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSerials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductVariantId = table.Column<int>(type: "integer", nullable: false),
                    SupplierPurchaseDetailsId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WarrantyMonths = table.Column<int>(type: "integer", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSerials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSerials_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSerials_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSerials_SupplierPurchaseDetails_SupplierPurchaseDeta~",
                        column: x => x.SupplierPurchaseDetailsId,
                        principalTable: "SupplierPurchaseDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductVariantId",
                table: "InventoryTransactions",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_BranchId",
                table: "ProductSerials",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_ProductVariantId",
                table: "ProductSerials",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_SerialNumber",
                table: "ProductSerials",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_SupplierPurchaseDetailsId",
                table: "ProductSerials",
                column: "SupplierPurchaseDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_AttributesJson",
                table: "ProductVariants",
                column: "AttributesJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SKU",
                table: "ProductVariants",
                column: "SKU",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_ProductVariants_ProductVariantId",
                table: "InventoryTransactions",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_SupplierPurchaseDetails_SupplierPurch~",
                table: "InventoryTransactions",
                column: "SupplierPurchaseDetailsId",
                principalTable: "SupplierPurchaseDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_ProductVariants_ProductVariantId",
                table: "Stocks",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchaseDetails_ProductVariants_ProductVariantId",
                table: "SupplierPurchaseDetails",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_ProductVariants_ProductVariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_SupplierPurchaseDetails_SupplierPurch~",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_ProductVariants_ProductVariantId",
                table: "Stocks");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchaseDetails_ProductVariants_ProductVariantId",
                table: "SupplierPurchaseDetails");

            migrationBuilder.DropTable(
                name: "ProductSerials");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ProductVariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "OrderedQuantity",
                table: "SupplierPurchaseDetails");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantity",
                table: "SupplierPurchaseDetails");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "InventoryTransactions");

            migrationBuilder.RenameColumn(
                name: "WarrantyMonths",
                table: "SupplierPurchaseDetails",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "SupplierPurchaseDetails",
                newName: "IsApproved");

            migrationBuilder.RenameColumn(
                name: "ProductVariantId",
                table: "SupplierPurchaseDetails",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchaseDetails_ProductVariantId",
                table: "SupplierPurchaseDetails",
                newName: "IX_SupplierPurchaseDetails_ProductId");

            migrationBuilder.RenameColumn(
                name: "ProductVariantId",
                table: "Stocks",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_ProductVariantId",
                table: "Stocks",
                newName: "IX_Stocks_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_BranchId_ProductVariantId",
                table: "Stocks",
                newName: "IX_Stocks_BranchId_ProductId");

            migrationBuilder.RenameColumn(
                name: "SupplierPurchaseDetailsId",
                table: "InventoryTransactions",
                newName: "SupplierPurchaseId");

            migrationBuilder.RenameColumn(
                name: "QuantityOut",
                table: "InventoryTransactions",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "QuantityIn",
                table: "InventoryTransactions",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_SupplierPurchaseDetailsId",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_SupplierPurchaseId");

            migrationBuilder.AlterColumn<string>(
                name: "PurchaseType",
                table: "SupplierPurchases",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductPrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_SupplierPurchases_SupplierPurchaseId",
                table: "InventoryTransactions",
                column: "SupplierPurchaseId",
                principalTable: "SupplierPurchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_Products_ProductId",
                table: "Stocks",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchaseDetails_Products_ProductId",
                table: "SupplierPurchaseDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
