using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapWars.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_guild_id",
                table: "products",
                column: "guild_id");

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_products_guild_id_name_lower"
                ON "products" ("guild_id", lower("name"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "UX_products_guild_id_name_lower";""");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
