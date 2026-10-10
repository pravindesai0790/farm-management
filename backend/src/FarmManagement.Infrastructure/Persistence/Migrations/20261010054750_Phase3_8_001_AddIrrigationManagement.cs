using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_8_001_AddIrrigationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "irrigation_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigation_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_irrigation_methods_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "irrigation_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plantation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    irrigation_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    planned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    planned_water_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    planned_water_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actual_water_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_water_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigation_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_irrigation_events_actual_water_unit",
                        column: x => x.actual_water_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_crop_cycle",
                        column: x => x.crop_cycle_id,
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_crop_cycle_stage",
                        column: x => x.crop_cycle_stage_id,
                        principalTable: "crop_cycle_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_farm_area",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_irrigation_method",
                        column: x => x.irrigation_method_id,
                        principalTable: "irrigation_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_planned_water_unit",
                        column: x => x.planned_water_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_irrigation_events_plantation",
                        column: x => x.plantation_id,
                        principalTable: "crop_plantations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_events_actual_water_unit_id",
                table: "irrigation_events",
                column: "actual_water_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_completed_at",
                table: "irrigation_events",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_crop_cycle_id",
                table: "irrigation_events",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_crop_cycle_stage_id",
                table: "irrigation_events",
                column: "crop_cycle_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_farm_area_id",
                table: "irrigation_events",
                column: "farm_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_farm_id",
                table: "irrigation_events",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_farm_status",
                table: "irrigation_events",
                columns: new[] { "farm_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_events_irrigation_method_id",
                table: "irrigation_events",
                column: "irrigation_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_organization_id",
                table: "irrigation_events",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_events_planned_water_unit_id",
                table: "irrigation_events",
                column: "planned_water_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_plantation_id",
                table: "irrigation_events",
                column: "plantation_id");

            migrationBuilder.CreateIndex(
                name: "ix_irrigation_events_scheduled_at",
                table: "irrigation_events",
                column: "scheduled_at");

            migrationBuilder.CreateIndex(
                name: "ux_irrigation_methods_organization_code",
                table: "irrigation_methods",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_irrigation_methods_system_code",
                table: "irrigation_methods",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "irrigation_events");

            migrationBuilder.DropTable(
                name: "irrigation_methods");
        }
    }
}
