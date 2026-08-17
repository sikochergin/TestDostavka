using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestDostavka.Migrations
{
    /// <inheritdoc />
    public partial class RequestQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "tbl_request",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quantity",
                table: "tbl_request");
        }
    }
}
