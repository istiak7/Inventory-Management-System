using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AuditUsersRestrictDeletesAndDataFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductSubCategories_ProductSubCategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubCategories_ProductCategories_ProductCategoryId",
                table: "ProductSubCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Branches_BranchId",
                table: "SupplierPayments");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "WarrantyClaims",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "WarrantyClaims",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "UserPermissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "UserPermissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "SupplierTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "SupplierTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "SupplierPurchases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "SupplierPurchases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "SupplierPurchasePayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "SupplierPurchasePayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "SupplierPurchaseDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "SupplierPurchaseDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "SupplierPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "SupplierPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "StockTransfers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "StockTransfers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "StockTransferDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "StockTransferDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Stocks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Stocks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "SaleDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "SaleDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "SaleCustomerPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "SaleCustomerPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "RolePermissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "RolePermissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "ProductVariants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "ProductVariants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "ProductSubCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "ProductSubCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "ProductSerials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "ProductSerials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "ProductCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "ProductCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Permissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Permissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "InventoryTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "InventoryTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Designations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Designations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Departments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Departments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "CustomerTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "CustomerTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "CustomerSales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "CustomerSales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "CustomerPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "CustomerPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Brands",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Brands",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductSubCategories_ProductSubCategoryId",
                table: "Products",
                column: "ProductSubCategoryId",
                principalTable: "ProductSubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubCategories_ProductCategories_ProductCategoryId",
                table: "ProductSubCategories",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Branches_BranchId",
                table: "SupplierPayments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---- Data fix 1: "IsActive" ----
            // New rows used to be saved with IsActive = 1, which in EntityStatus means "InActive"
            // (0 = Active, 1 = InActive, 2 = Deleted). Nothing in the system set "InActive" on
            // purpose except on users, so every other table is set back to Active.
            migrationBuilder.Sql("UPDATE \"Branches\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Brands\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"CustomerPayments\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"CustomerSales\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"CustomerTransactions\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Customers\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Departments\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Designations\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"InventoryTransactions\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Permissions\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"ProductCategories\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"ProductSerials\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"ProductSubCategories\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"ProductVariants\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"RolePermissions\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Roles\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"SaleCustomerPayments\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"SaleDetails\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Stocks\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"StockTransferDetails\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"StockTransfers\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"SupplierPayments\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"SupplierPurchaseDetails\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"SupplierPurchasePayments\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"SupplierPurchases\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"SupplierTransactions\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"Suppliers\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"UserPermissions\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");
            migrationBuilder.Sql("UPDATE \"WarrantyClaims\" SET \"IsActive\" = 0 WHERE \"IsActive\" = 1;");

            // ---- Data fix 2: opening balances go into the ledger ----
            // Customers and suppliers created with an opening balance never had it in their ledger,
            // so it was missing from their balance and could not be paid. Add one "Opening" row per
            // party. It is added after the existing rows, so its BalanceAfter is the latest balance
            // plus the opening balance (the running balance stays correct).
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerTransactions""
                    (""CustomerId"", ""TransactionType"", ""TransactionDate"", ""Debit"", ""Credit"", ""BalanceAfter"",
                     ""CreatedAt"", ""UpDatedAt"", ""IsActive"")
                SELECT c.""Id"", 'Opening', c.""CreatedAt"", 0, c.""OpeningBalance"",
                       COALESCE((SELECT t.""BalanceAfter"" FROM ""CustomerTransactions"" t
                                 WHERE t.""CustomerId"" = c.""Id"" ORDER BY t.""Id"" DESC LIMIT 1), 0) + c.""OpeningBalance"",
                       now(), now(), 0
                FROM ""Customers"" c
                WHERE c.""OpeningBalance"" <> 0
                  AND NOT EXISTS (SELECT 1 FROM ""CustomerTransactions"" t
                                  WHERE t.""CustomerId"" = c.""Id"" AND t.""TransactionType"" = 'Opening');");

            migrationBuilder.Sql(@"
                INSERT INTO ""SupplierTransactions""
                    (""SupplierId"", ""TransactionType"", ""TransactionDate"", ""Debit"", ""Credit"", ""BalanceAfter"",
                     ""CreatedAt"", ""UpDatedAt"", ""IsActive"")
                SELECT s.""Id"", 'Opening', s.""CreatedAt"", s.""OpeningBalance"", 0,
                       COALESCE((SELECT t.""BalanceAfter"" FROM ""SupplierTransactions"" t
                                 WHERE t.""SupplierId"" = s.""Id"" ORDER BY t.""Id"" DESC LIMIT 1), 0) + s.""OpeningBalance"",
                       now(), now(), 0
                FROM ""Suppliers"" s
                WHERE s.""OpeningBalance"" <> 0
                  AND NOT EXISTS (SELECT 1 FROM ""SupplierTransactions"" t
                                  WHERE t.""SupplierId"" = s.""Id"" AND t.""TransactionType"" = 'Opening');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductSubCategories_ProductSubCategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubCategories_ProductCategories_ProductCategoryId",
                table: "ProductSubCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Branches_BranchId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SupplierTransactions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SupplierTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SupplierPurchases");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SupplierPurchases");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SupplierPurchasePayments");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SupplierPurchasePayments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SupplierPurchaseDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SupplierPurchaseDetails");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "StockTransferDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "StockTransferDetails");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SaleCustomerPayments");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SaleCustomerPayments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ProductSubCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ProductSubCategories");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ProductSerials");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ProductSerials");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Designations");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Designations");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "CustomerTransactions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "CustomerTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "CustomerSales");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "CustomerSales");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Branches");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductSubCategories_ProductSubCategoryId",
                table: "Products",
                column: "ProductSubCategoryId",
                principalTable: "ProductSubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubCategories_ProductCategories_ProductCategoryId",
                table: "ProductSubCategories",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Branches_BranchId",
                table: "SupplierPayments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
