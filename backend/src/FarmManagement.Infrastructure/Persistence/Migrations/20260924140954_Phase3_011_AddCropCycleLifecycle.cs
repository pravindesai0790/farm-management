using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_011_AddCropCycleLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stage_code",
                table: "crop_lifecycle_stages");

            migrationBuilder.AddColumn<int>(
                name: "expected_duration_days",
                table: "crop_lifecycle_stages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "lifecycle_template_id",
                table: "crop_cycles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "crop_cycle_stages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lifecycle_template_stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    expected_duration_days = table.Column<int>(type: "integer", nullable: true),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    planned_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crop_cycle_stages", x => x.id);
                    table.CheckConstraint("ck_crop_cycle_stages_actual_dates", "actual_end_date IS NULL OR actual_start_date IS NULL OR actual_end_date >= actual_start_date");
                    table.CheckConstraint("ck_crop_cycle_stages_expected_duration_days", "expected_duration_days IS NULL OR expected_duration_days > 0");
                    table.CheckConstraint("ck_crop_cycle_stages_planned_dates", "planned_end_date IS NULL OR planned_start_date IS NULL OR planned_end_date >= planned_start_date");
                    table.CheckConstraint("ck_crop_cycle_stages_sequence_number", "sequence_number > 0");
                    table.CheckConstraint("ck_crop_cycle_stages_status", "status IN ('NOT_STARTED', 'IN_PROGRESS', 'COMPLETED', 'SKIPPED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_crop_cycle_stage_crop_cycle",
                        column: x => x.crop_cycle_id,
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_crop_cycle_stage_template_stage",
                        column: x => x.lifecycle_template_stage_id,
                        principalTable: "crop_lifecycle_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_crop_lifecycle_stages_expected_duration_days",
                table: "crop_lifecycle_stages",
                sql: "expected_duration_days IS NULL OR expected_duration_days > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_crop_lifecycle_stages_sequence_number",
                table: "crop_lifecycle_stages",
                sql: "sequence_number > 0");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_lifecycle_template_id",
                table: "crop_cycles",
                column: "lifecycle_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_stages_crop_cycle_id",
                table: "crop_cycle_stages",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_stages_cycle_status",
                table: "crop_cycle_stages",
                columns: new[] { "crop_cycle_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_stages_lifecycle_template_stage_id",
                table: "crop_cycle_stages",
                column: "lifecycle_template_stage_id");

            migrationBuilder.CreateIndex(
                name: "ux_crop_cycle_stages_cycle_sequence",
                table: "crop_cycle_stages",
                columns: new[] { "crop_cycle_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_crop_cycle_stages_cycle_template_stage",
                table: "crop_cycle_stages",
                columns: new[] { "crop_cycle_id", "lifecycle_template_stage_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_crop_cycle_lifecycle_template",
                table: "crop_cycles",
                column: "lifecycle_template_id",
                principalTable: "crop_lifecycle_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_crop_cycle_lifecycle_template",
                table: "crop_cycles");

            migrationBuilder.DropTable(
                name: "crop_cycle_stages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_crop_lifecycle_stages_expected_duration_days",
                table: "crop_lifecycle_stages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_crop_lifecycle_stages_sequence_number",
                table: "crop_lifecycle_stages");

            migrationBuilder.DropIndex(
                name: "ix_crop_cycles_lifecycle_template_id",
                table: "crop_cycles");

            migrationBuilder.DropColumn(
                name: "expected_duration_days",
                table: "crop_lifecycle_stages");

            migrationBuilder.DropColumn(
                name: "lifecycle_template_id",
                table: "crop_cycles");

            migrationBuilder.AddColumn<string>(
                name: "stage_code",
                table: "crop_lifecycle_stages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
