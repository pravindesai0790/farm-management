using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_015_RemoveCropCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_crops_organization_code",
                table: "crops");

            migrationBuilder.DropIndex(
                name: "ux_crops_system_code",
                table: "crops");

            migrationBuilder.DropColumn(
                name: "code",
                table: "crops");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "crops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_crops_organization_code",
                table: "crops",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_crops_system_code",
                table: "crops",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");
        }
    }
}
