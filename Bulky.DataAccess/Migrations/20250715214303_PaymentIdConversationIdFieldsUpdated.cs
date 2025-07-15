using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PaymentIdConversationIdFieldsUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "OrderHeader",
                newName: "PaymentId");

            migrationBuilder.RenameColumn(
                name: "PaymentIntentId",
                table: "OrderHeader",
                newName: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "OrderHeader",
                newName: "SessionId");

            migrationBuilder.RenameColumn(
                name: "ConversationId",
                table: "OrderHeader",
                newName: "PaymentIntentId");
        }
    }
}
