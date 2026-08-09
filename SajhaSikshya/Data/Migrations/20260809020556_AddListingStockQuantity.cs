using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SajhaSikshya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingStockQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Listings");
        }
    }
}
