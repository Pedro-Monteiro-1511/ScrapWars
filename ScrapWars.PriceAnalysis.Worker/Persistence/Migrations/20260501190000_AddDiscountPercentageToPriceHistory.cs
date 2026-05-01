using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapWars.PriceAnalysis.Worker.Persistence.Migrations
{
    public partial class AddDiscountPercentageToPriceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "discount_percentage",
                table: "product_price_history",
                type: "numeric(5,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discount_percentage",
                table: "product_price_history");
        }
    }
}
