using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SajhaSikshya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsPublicProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublicProfile",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublicProfile",
                table: "Users");
        }
    }
}
