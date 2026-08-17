using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestDostavka.Migrations
{
    /// <inheritdoc />
    public partial class requestcomment_commentnamechange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "tbl_request_comment",
                newName: "сomment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "сomment",
                table: "tbl_request_comment",
                newName: "Comment");
        }
    }
}
