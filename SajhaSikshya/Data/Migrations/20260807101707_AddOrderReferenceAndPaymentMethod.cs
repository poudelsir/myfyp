using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SajhaSikshya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReferenceAndPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Added nullable first: existing Order rows (from Phase 6.1 testing) need a
            // real, unique backfilled value before the column can become NOT NULL with a
            // unique index — a flat default of "" would collide across every existing row.
            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Orders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Orders
                SET ReferenceNumber = 'ORD-' + CAST(YEAR(CreatedAtUtc) AS varchar(4)) + '-' + RIGHT('000000' + CAST(Id AS varchar(10)), 6),
                    PaymentMethod = CASE WHEN IsDonation = 1 THEN 0 ELSE 1 END
                WHERE ReferenceNumber IS NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "Orders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReferenceNumber",
                table: "Orders",
                column: "ReferenceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ReferenceNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Orders");
        }
    }
}
