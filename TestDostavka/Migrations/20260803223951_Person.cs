using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestDostavka.Migrations
{
    /// <inheritdoc />
    public partial class Person : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_person",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creationdatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    mail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_person", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_person");
        }
    }
}
