using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarrantyClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClaimNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductSerialId = table.Column<int>(type: "integer", nullable: false),
                    SaleDetailsId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    DefectDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AccessoriesReceived = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TechnicianName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReplacementSerialId = table.Column<int>(type: "integer", nullable: true),
                    ClaimDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepairStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpDatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_ProductSerials_ProductSerialId",
                        column: x => x.ProductSerialId,
                        principalTable: "ProductSerials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_ProductSerials_ReplacementSerialId",
                        column: x => x.ReplacementSerialId,
                        principalTable: "ProductSerials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_SaleDetails_SaleDetailsId",
                        column: x => x.SaleDetailsId,
                        principalTable: "SaleDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_BranchId",
                table: "WarrantyClaims",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ClaimNumber",
                table: "WarrantyClaims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_CustomerId",
                table: "WarrantyClaims",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ProductSerialId",
                table: "WarrantyClaims",
                column: "ProductSerialId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ReplacementSerialId",
                table: "WarrantyClaims",
                column: "ReplacementSerialId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_SaleDetailsId",
                table: "WarrantyClaims",
                column: "SaleDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_Status",
                table: "WarrantyClaims",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarrantyClaims");
        }
    }
}
