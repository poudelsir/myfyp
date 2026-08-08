using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SajhaSikshya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingModerationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastModeratedAtUtc",
                table: "Listings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModeratedByUserId",
                table: "Listings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastModerationAction",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                table: "Listings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_LastModeratedByUserId",
                table: "Listings",
                column: "LastModeratedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Users_LastModeratedByUserId",
                table: "Listings",
                column: "LastModeratedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Users_LastModeratedByUserId",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_LastModeratedByUserId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "LastModeratedAtUtc",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "LastModeratedByUserId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "LastModerationAction",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "Listings");
        }
    }
}
