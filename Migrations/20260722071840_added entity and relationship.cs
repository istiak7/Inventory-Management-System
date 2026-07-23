using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class addedentityandrelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayment_Suppliers_SupplierId",
                table: "SupplierPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchase_Suppliers_SupplierId",
                table: "SupplierPurchase");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchasePayment_SupplierPayment_SupplierPaymentId",
                table: "SupplierPurchasePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchasePayment_SupplierPurchase_SupplierPurchaseId",
                table: "SupplierPurchasePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierTransaction_SupplierPayment_SupplierPaymentId",
                table: "SupplierTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierTransaction_SupplierPurchase_SupplierPurchaseId",
                table: "SupplierTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierTransaction_Suppliers_SupplierId",
                table: "SupplierTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierTransaction",
                table: "SupplierTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPurchasePayment",
                table: "SupplierPurchasePayment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPurchase",
                table: "SupplierPurchase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPayment",
                table: "SupplierPayment");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "SupplierTransaction");

            migrationBuilder.DropColumn(
                name: "DueAmount",
                table: "SupplierPurchase");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "SupplierPurchase");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "SupplierTransaction",
                newName: "SupplierTransactions");

            migrationBuilder.RenameTable(
                name: "SupplierPurchasePayment",
                newName: "SupplierPurchasePayments");

            migrationBuilder.RenameTable(
                name: "SupplierPurchase",
                newName: "SupplierPurchases");

            migrationBuilder.RenameTable(
                name: "SupplierPayment",
                newName: "SupplierPayments");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "Products",
                newName: "SKU");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierTransaction_SupplierPurchaseId",
                table: "SupplierTransactions",
                newName: "IX_SupplierTransactions_SupplierPurchaseId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierTransaction_SupplierPaymentId",
                table: "SupplierTransactions",
                newName: "IX_SupplierTransactions_SupplierPaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierTransaction_SupplierId",
                table: "SupplierTransactions",
                newName: "IX_SupplierTransactions_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchasePayment_SupplierPurchaseId",
                table: "SupplierPurchasePayments",
                newName: "IX_SupplierPurchasePayments_SupplierPurchaseId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchasePayment_SupplierPaymentId",
                table: "SupplierPurchasePayments",
                newName: "IX_SupplierPurchasePayments_SupplierPaymentId");

            migrationBuilder.RenameColumn(
                name: "Invoice",
                table: "SupplierPurchases",
                newName: "PurchaseType");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchase_SupplierId",
                table: "SupplierPurchases",
                newName: "IX_SupplierPurchases_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayment_SupplierId",
                table: "SupplierPayments",
                newName: "IX_SupplierPayments_SupplierId");

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningBalance",
                table: "Suppliers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "ProductPrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "SupplierTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierPurchaseId",
                table: "SupplierTransactions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierPaymentId",
                table: "SupplierTransactions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceAfter",
                table: "SupplierTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Credit",
                table: "SupplierTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Debit",
                table: "SupplierTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplierPurchasePayments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<DateTime>(
                name: "AllocationDate",
                table: "SupplierPurchasePayments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupplierPurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SupplierPurchases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "SupplierPurchases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "SupplierPayments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplierPayments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SupplierPayments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierTransactions",
                table: "SupplierTransactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPurchasePayments",
                table: "SupplierPurchasePayments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPurchases",
                table: "SupplierPurchases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPayments",
                table: "SupplierPayments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Designations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Designations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPurchaseDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsApproved = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPurchaseDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPurchaseDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPurchaseDetails_SupplierPurchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "SupplierPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SupplierPurchaseId = table.Column<int>(type: "integer", nullable: true),
                    TransactionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_SupplierPurchases_SupplierPurchaseId",
                        column: x => x.SupplierPurchaseId,
                        principalTable: "SupplierPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    CurrentStock = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocks_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPurchases_BranchId",
                table: "SupplierPurchases",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_BranchId",
                table: "SupplierPayments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_BranchId",
                table: "InventoryTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_SupplierPurchaseId",
                table: "InventoryTransactions",
                column: "SupplierPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_BranchId_ProductId",
                table: "Stocks",
                columns: new[] { "BranchId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId",
                table: "Stocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPurchaseDetails_ProductId",
                table: "SupplierPurchaseDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPurchaseDetails_PurchaseId",
                table: "SupplierPurchaseDetails",
                column: "PurchaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Branches_BranchId",
                table: "SupplierPayments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchasePayments_SupplierPayments_SupplierPaymentId",
                table: "SupplierPurchasePayments",
                column: "SupplierPaymentId",
                principalTable: "SupplierPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchasePayments_SupplierPurchases_SupplierPurchase~",
                table: "SupplierPurchasePayments",
                column: "SupplierPurchaseId",
                principalTable: "SupplierPurchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchases_Branches_BranchId",
                table: "SupplierPurchases",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchases_Suppliers_SupplierId",
                table: "SupplierPurchases",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierTransactions_SupplierPayments_SupplierPaymentId",
                table: "SupplierTransactions",
                column: "SupplierPaymentId",
                principalTable: "SupplierPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierTransactions_SupplierPurchases_SupplierPurchaseId",
                table: "SupplierTransactions",
                column: "SupplierPurchaseId",
                principalTable: "SupplierPurchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierTransactions_Suppliers_SupplierId",
                table: "SupplierTransactions",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Branches_BranchId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchasePayments_SupplierPayments_SupplierPaymentId",
                table: "SupplierPurchasePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchasePayments_SupplierPurchases_SupplierPurchase~",
                table: "SupplierPurchasePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchases_Branches_BranchId",
                table: "SupplierPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPurchases_Suppliers_SupplierId",
                table: "SupplierPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierTransactions_SupplierPayments_SupplierPaymentId",
                table: "SupplierTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierTransactions_SupplierPurchases_SupplierPurchaseId",
                table: "SupplierTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierTransactions_Suppliers_SupplierId",
                table: "SupplierTransactions");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Designations");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "SupplierPurchaseDetails");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Products_SKU",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierTransactions",
                table: "SupplierTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPurchases",
                table: "SupplierPurchases");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPurchases_BranchId",
                table: "SupplierPurchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPurchasePayments",
                table: "SupplierPurchasePayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPayments",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_BranchId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                table: "SupplierTransactions");

            migrationBuilder.DropColumn(
                name: "Credit",
                table: "SupplierTransactions");

            migrationBuilder.DropColumn(
                name: "Debit",
                table: "SupplierTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SupplierPurchases");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "SupplierPurchases");

            migrationBuilder.DropColumn(
                name: "AllocationDate",
                table: "SupplierPurchasePayments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SupplierPayments");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "SupplierTransactions",
                newName: "SupplierTransaction");

            migrationBuilder.RenameTable(
                name: "SupplierPurchases",
                newName: "SupplierPurchase");

            migrationBuilder.RenameTable(
                name: "SupplierPurchasePayments",
                newName: "SupplierPurchasePayment");

            migrationBuilder.RenameTable(
                name: "SupplierPayments",
                newName: "SupplierPayment");

            migrationBuilder.RenameColumn(
                name: "SKU",
                table: "Products",
                newName: "ProductCode");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierTransactions_SupplierPurchaseId",
                table: "SupplierTransaction",
                newName: "IX_SupplierTransaction_SupplierPurchaseId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierTransactions_SupplierPaymentId",
                table: "SupplierTransaction",
                newName: "IX_SupplierTransaction_SupplierPaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierTransactions_SupplierId",
                table: "SupplierTransaction",
                newName: "IX_SupplierTransaction_SupplierId");

            migrationBuilder.RenameColumn(
                name: "PurchaseType",
                table: "SupplierPurchase",
                newName: "Invoice");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchases_SupplierId",
                table: "SupplierPurchase",
                newName: "IX_SupplierPurchase_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchasePayments_SupplierPurchaseId",
                table: "SupplierPurchasePayment",
                newName: "IX_SupplierPurchasePayment_SupplierPurchaseId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPurchasePayments_SupplierPaymentId",
                table: "SupplierPurchasePayment",
                newName: "IX_SupplierPurchasePayment_SupplierPaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayments_SupplierId",
                table: "SupplierPayment",
                newName: "IX_SupplierPayment_SupplierId");

            migrationBuilder.AlterColumn<int>(
                name: "OpeningBalance",
                table: "Suppliers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ProductPrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "SupplierTransaction",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "SupplierPurchaseId",
                table: "SupplierTransaction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SupplierPaymentId",
                table: "SupplierTransaction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "SupplierTransaction",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupplierPurchase",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "DueAmount",
                table: "SupplierPurchase",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "SupplierPurchase",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplierPurchasePayment",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "SupplierPayment",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplierPayment",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierTransaction",
                table: "SupplierTransaction",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPurchase",
                table: "SupplierPurchase",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPurchasePayment",
                table: "SupplierPurchasePayment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPayment",
                table: "SupplierPayment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayment_Suppliers_SupplierId",
                table: "SupplierPayment",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchase_Suppliers_SupplierId",
                table: "SupplierPurchase",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchasePayment_SupplierPayment_SupplierPaymentId",
                table: "SupplierPurchasePayment",
                column: "SupplierPaymentId",
                principalTable: "SupplierPayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPurchasePayment_SupplierPurchase_SupplierPurchaseId",
                table: "SupplierPurchasePayment",
                column: "SupplierPurchaseId",
                principalTable: "SupplierPurchase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierTransaction_SupplierPayment_SupplierPaymentId",
                table: "SupplierTransaction",
                column: "SupplierPaymentId",
                principalTable: "SupplierPayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierTransaction_SupplierPurchase_SupplierPurchaseId",
                table: "SupplierTransaction",
                column: "SupplierPurchaseId",
                principalTable: "SupplierPurchase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierTransaction_Suppliers_SupplierId",
                table: "SupplierTransaction",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
