using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_013_RemoveFarmAreaCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_farm_area_farm_code",
                table: "farm_areas");

            migrationBuilder.DropColumn(
                name: "code",
                table: "farm_areas");

            migrationBuilder.CreateIndex(
                name: "ix_farm_areas_farm_id",
                table: "farm_areas",
                column: "farm_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_farm_areas_farm_id",
                table: "farm_areas");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "farm_areas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_farm_area_farm_code",
                table: "farm_areas",
                columns: new[] { "farm_id", "code" },
                unique: true);
        }
    }
}
