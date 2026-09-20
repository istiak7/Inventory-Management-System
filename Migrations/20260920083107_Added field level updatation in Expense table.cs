using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddedfieldlevelupdatationinExpensetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_RecordByUserId",
                table: "Expenses");

            migrationBuilder.AlterColumn<int>(
                name: "RecordByUserId",
                table: "Expenses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_RecordByUserId",
                table: "Expenses",
                column: "RecordByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_RecordByUserId",
                table: "Expenses");

            migrationBuilder.AlterColumn<int>(
                name: "RecordByUserId",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_RecordByUserId",
                table: "Expenses",
                column: "RecordByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
