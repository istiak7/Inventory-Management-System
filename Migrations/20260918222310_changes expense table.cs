using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class changesexpensetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Expenses_RecordByUserId",
                table: "Expenses",
                column: "RecordByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_RecordByUserId",
                table: "Expenses",
                column: "RecordByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_RecordByUserId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_RecordByUserId",
                table: "Expenses");
        }
    }
}
