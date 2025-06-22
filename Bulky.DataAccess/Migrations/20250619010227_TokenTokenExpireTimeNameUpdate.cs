using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TokenTokenExpireTimeNameUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IntentIdExpireTime",
                table: "OrderHeader",
                newName: "TokenExpireTime");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "OrderHeader",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "OrderHeader");

            migrationBuilder.RenameColumn(
                name: "TokenExpireTime",
                table: "OrderHeader",
                newName: "IntentIdExpireTime");
        }
    }
}
