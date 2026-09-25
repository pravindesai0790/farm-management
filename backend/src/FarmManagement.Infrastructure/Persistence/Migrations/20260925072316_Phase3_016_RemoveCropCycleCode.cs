using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_016_RemoveCropCycleCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_crop_cycle_organization_code",
                table: "crop_cycles");

            migrationBuilder.DropColumn(
                name: "cycle_code",
                table: "crop_cycles");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_organization_id",
                table: "crop_cycles",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_crop_cycles_organization_id",
                table: "crop_cycles");

            migrationBuilder.AddColumn<string>(
                name: "cycle_code",
                table: "crop_cycles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_crop_cycle_organization_code",
                table: "crop_cycles",
                columns: new[] { "organization_id", "cycle_code" },
                unique: true);
        }
    }
}
