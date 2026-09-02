using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations;

public partial class Phase2_009_AddCropCycles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "crop_cycles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                plantation_id = table.Column<Guid>(type: "uuid", nullable: false),
                cycle_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                cycle_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                season_year = table.Column<int>(type: "integer", nullable: false),
                season_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                planned_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                actual_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                expected_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                actual_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                cancellation_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                cancellation_notes = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_crop_cycles", x => x.id);
                table.ForeignKey("fk_crop_cycle_organization", x => x.organization_id, "organizations", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_crop_cycle_plantation", x => x.plantation_id, "crop_plantations", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_crop_cycle_cancellation_reason", x => x.cancellation_reason_id, "plantation_end_reasons", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_crop_cycles_cancellation_reason_id",
            table: "crop_cycles",
            column: "cancellation_reason_id");
        migrationBuilder.CreateIndex(
            name: "ix_crop_cycles_plantation_status",
            table: "crop_cycles",
            columns: new[] { "plantation_id", "status" });
        migrationBuilder.CreateIndex(
            name: "ux_crop_cycle_organization_code",
            table: "crop_cycles",
            columns: new[] { "organization_id", "cycle_code" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "crop_cycles");
}
