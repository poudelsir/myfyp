using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SajhaSikshya.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefineSellerVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StudentIdImagePath",
                table: "StudentVerifications",
                newName: "GovernmentIdImagePath");

            migrationBuilder.AlterColumn<int>(
                name: "UniversityId",
                table: "StudentVerifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "StudentNumber",
                table: "StudentVerifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "AcademicIdDocumentType",
                table: "StudentVerifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademicIdImagePath",
                table: "StudentVerifications",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeclarationAccepted",
                table: "StudentVerifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GovernmentIdDocumentType",
                table: "StudentVerifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoImagePath",
                table: "StudentVerifications",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellerType",
                table: "StudentVerifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellingCategoriesCsv",
                table: "StudentVerifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicIdDocumentType",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "AcademicIdImagePath",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "DeclarationAccepted",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "GovernmentIdDocumentType",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoImagePath",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "SellerType",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "SellingCategoriesCsv",
                table: "StudentVerifications");

            migrationBuilder.RenameColumn(
                name: "GovernmentIdImagePath",
                table: "StudentVerifications",
                newName: "StudentIdImagePath");

            migrationBuilder.AlterColumn<int>(
                name: "UniversityId",
                table: "StudentVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StudentNumber",
                table: "StudentVerifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
