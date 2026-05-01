using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapWars.PriceAnalysis.Worker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_price_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    site_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    business_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    captured_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_price_history", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_price_history_product_id_captured_at_utc",
                table: "product_price_history",
                columns: new[] { "product_id", "captured_at_utc" });

            migrationBuilder.CreateIndex(
                name: "UX_product_price_history_source_event_id",
                table: "product_price_history",
                column: "source_event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_price_history");
        }
    }
}
