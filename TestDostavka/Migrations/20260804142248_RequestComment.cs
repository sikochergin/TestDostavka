using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestDostavka.Migrations
{
    /// <inheritdoc />
    public partial class RequestComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_request_comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creationdatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_from_customer = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_request_comment", x => x.id);
                    table.ForeignKey(
                        name: "FK_tbl_request_comment_tbl_request_request_id",
                        column: x => x.request_id,
                        principalTable: "tbl_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_request_comment_request_id",
                table: "tbl_request_comment",
                column: "request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_request_comment");
        }
    }
}
