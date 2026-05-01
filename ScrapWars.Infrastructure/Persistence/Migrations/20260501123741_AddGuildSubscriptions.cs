using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapWars.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guild_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    extra_channels = table.Column<int>(type: "integer", nullable: false),
                    extra_categories = table.Column<int>(type: "integer", nullable: false),
                    extra_products = table.Column<int>(type: "integer", nullable: false),
                    extra_channel_unit_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    extra_category_unit_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    extra_product_unit_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_subscriptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_guild_subscriptions_guild_id",
                table: "guild_subscriptions",
                column: "guild_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_subscriptions");
        }
    }
}
