using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_012_RemoveFarmCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_farm_organization_code",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "code",
                table: "farms");

            migrationBuilder.CreateIndex(
                name: "ix_farms_organization_id",
                table: "farms",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_farms_organization_id",
                table: "farms");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "farms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_farm_organization_code",
                table: "farms",
                columns: new[] { "organization_id", "code" },
                unique: true);
        }
    }
}
