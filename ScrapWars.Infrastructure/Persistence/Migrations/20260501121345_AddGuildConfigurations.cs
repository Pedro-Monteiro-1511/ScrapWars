using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapWars.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category_notification_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_notification_channels", x => x.id);
                    table.ForeignKey(
                        name: "FK_category_notification_channels_product_categories_category_~",
                        column: x => x.category_id,
                        principalTable: "product_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_notification_channels_guild_id",
                table: "category_notification_channels",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "UX_category_notification_channels_category_id_channel_id",
                table: "category_notification_channels",
                columns: new[] { "category_id", "channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_guild_id",
                table: "product_categories",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "UX_product_categories_guild_id_normalized_name",
                table: "product_categories",
                columns: new[] { "guild_id", "normalized_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_products_product_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "product_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_product_categories_category_id",
                table: "products");

            migrationBuilder.DropTable(
                name: "category_notification_channels");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropIndex(
                name: "IX_products_category_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "products");
        }
    }
}
