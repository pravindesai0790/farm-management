using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_014_RemovePlantationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_plantation_organization_code",
                table: "crop_plantations");

            migrationBuilder.DropColumn(
                name: "plantation_code",
                table: "crop_plantations");

            migrationBuilder.CreateIndex(
                name: "ix_crop_plantations_organization_id",
                table: "crop_plantations",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_crop_plantations_organization_id",
                table: "crop_plantations");

            migrationBuilder.AddColumn<string>(
                name: "plantation_code",
                table: "crop_plantations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_plantation_organization_code",
                table: "crop_plantations",
                columns: new[] { "organization_id", "plantation_code" },
                unique: true);
        }
    }
}
