using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestDostavka.Migrations
{
    /// <inheritdoc />
    public partial class techcomment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tech_comment",
                table: "tbl_request_comment",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tech_comment",
                table: "tbl_request_comment");
        }
    }
}
