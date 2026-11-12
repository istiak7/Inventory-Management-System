using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddPartyLinkAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Email",
                table: "Suppliers");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Suppliers",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartySettlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    CustomerPaymentId = table.Column<int>(type: "integer", nullable: false),
                    SupplierPaymentId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceSaleId = table.Column<int>(type: "integer", nullable: true),
                    SourcePurchaseId = table.Column<int>(type: "integer", nullable: true),
                    ReversalOfId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: true),
                    UpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartySettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartySettlements_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_CustomerSales_SourceSaleId",
                        column: x => x.SourceSaleId,
                        principalTable: "CustomerSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_PartySettlements_ReversalOfId",
                        column: x => x.ReversalOfId,
                        principalTable: "PartySettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_SupplierPayments_SupplierPaymentId",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_SupplierPurchases_SourcePurchaseId",
                        column: x => x.SourcePurchaseId,
                        principalTable: "SupplierPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartySettlements_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Email",
                table: "Suppliers",
                column: "Email",
                unique: true,
                filter: "\"Email\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_PhoneNumber",
                table: "Suppliers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SupplierId",
                table: "Customers",
                column: "SupplierId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_CustomerId",
                table: "PartySettlements",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_CustomerPaymentId",
                table: "PartySettlements",
                column: "CustomerPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_ReversalOfId",
                table: "PartySettlements",
                column: "ReversalOfId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_SourcePurchaseId",
                table: "PartySettlements",
                column: "SourcePurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_SourceSaleId",
                table: "PartySettlements",
                column: "SourceSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_SupplierId",
                table: "PartySettlements",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PartySettlements_SupplierPaymentId",
                table: "PartySettlements",
                column: "SupplierPaymentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Suppliers_SupplierId",
                table: "Customers",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Suppliers_SupplierId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "PartySettlements");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Email",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_PhoneNumber",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_SupplierId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Email",
                table: "Suppliers",
                column: "Email",
                unique: true);
        }
    }
}
